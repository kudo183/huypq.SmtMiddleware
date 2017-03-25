﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace huypq.SmtMiddleware
{
    public class SmtRouter<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>
        where TenantEntityType : class, SmtITenant, new()
        where UserEntityType : class, SmtIUser, new()
        where UserClaimEntityType : class, SmtIUserClaim
        where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
    {
        public RouteHandler GetRouteHandler()
        {
            return _router;
        }
        RouteHandler _router;

        private IApplicationBuilder _app;

        private string _controllerNamespacePattern;
        private readonly string _smtController = "smt";
        private readonly string _register = "register";
        private readonly string _tenantLogin = "tenantlogin";
        private readonly string _userLogin = "userlogin";

        public SmtRouter(IApplicationBuilder app, string controllerNamespacePattern)
        {
            _app = app;
            _controllerNamespacePattern = controllerNamespacePattern;
            _router = new RouteHandler(SmtRouteHandler);
        }

        private async Task SmtRouteHandler(HttpContext context)
        {
            try
            {
                var routeValues = context.GetRouteData().Values;
                var controller = routeValues["controller"]?.ToString().ToLower();
                var action = routeValues["action"]?.ToString().ToLower();

                var parameter = GetRequestParameter(context.Request);

                var requestType = "json";
                if (context.Request.Headers["request"].Count == 1)
                {
                    requestType = context.Request.Headers["request"][0];
                }

                var result = RequestExecutor(controller, action, parameter, context, requestType);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    context.Response.StatusCode = (int)result.StatusCode;
                    return;
                }

                var responseType = "json";
                if (context.Request.Headers["response"].Count == 1)
                {
                    responseType = context.Request.Headers["response"][0];
                }

                await WriteResponse(context.Response, responseType, result);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return;
            }
        }

        private Dictionary<string, object> GetRequestParameter(HttpRequest request)
        {
            var parameter = new Dictionary<string, object>();

            foreach (var q in request.Query)
            {
                parameter.Add(q.Key, q.Value);
            }

            if (request.HasFormContentType)
            {
                foreach (var f in request.Form)
                {
                    parameter.Add(f.Key, f.Value);
                }

                foreach (var file in request.Form.Files)
                {
                    parameter.Add(file.Name, file);
                }
            }

            parameter.Add("body", request.Body);

            return parameter;
        }

        private SmtActionResult RequestExecutor(
            string controller, string action, Dictionary<string, object> parameter,
            HttpContext context, string requestType)
        {
            SmtActionResult result = null;
            var request = context.Request;

            try
            {
                if (_smtController == controller)
                {
                    if (_register == action || _tenantLogin == action || _userLogin == action)//skip check token
                    {
                        result = ControllerInvoker(controller, action, parameter, null, requestType, context);
                        if (_tenantLogin == action || _userLogin == action)
                        {
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var protector = _app.ApplicationServices.GetDataProtector("token");
                                var base64PlainToken = (result.ResultValue as SmtTokenModel).ToBase64();
                                result.ResultValue = protector.Protect(base64PlainToken);
                            }
                        }
                    }
                    else
                    {
                        var tokenText = request.Headers["token"][0];
                        result = ControllerInvoker(controller, action, parameter, tokenText, requestType, context);
                    }
                }
                else if (SmtSettings.Instance.AllowAnonymousActions.Contains(string.Format("{0}.{1}", controller, action)))
                {
                    result = ControllerInvoker(controller, action, parameter, null, requestType, context);
                }
                else
                {
                    var tokenText = request.Headers["token"][0];
                    result = ControllerInvoker(controller, action, parameter, tokenText, requestType, context);
                }
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                result = new SmtActionResult
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized
                };
            }
            catch (Exception ex)
            {
                result = new SmtActionResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            return result;
        }

        private SmtActionResult ControllerInvoker(
            string controllerName, string actionName, Dictionary<string, object> parameter,
            string tokenText, string requestType, HttpContext context)
        {
            SmtTokenModel token = null;
            if (string.IsNullOrEmpty(tokenText) == false)
            {
                var protector = _app.ApplicationServices.GetDataProtector("token");

                var base64PlainToken = protector.Unprotect(tokenText);
                token = SmtTokenModel.FromBase64(base64PlainToken);
                if (CheckTokenValid(token, context) == false)
                {
                    return new SmtActionResult { StatusCode = System.Net.HttpStatusCode.Unauthorized };
                }
                if (CheckActionPermission(controllerName, actionName, token) == false)
                {
                    return new SmtActionResult { StatusCode = System.Net.HttpStatusCode.Unauthorized };
                }
            }

            var controllerType = Type.GetType(
                string.Format(_controllerNamespacePattern, controllerName), true, true);

            var controller = Activator.CreateInstance(controllerType) as SmtAbstractController;
            controller.Init(token, _app, context, requestType);
            return controller.ActionInvoker(actionName, parameter);
        }

        private async Task WriteResponse(HttpResponse response, string responseType, SmtActionResult result)
        {
            if (result.ResultValue == null)
            {
                response.StatusCode = (int)result.StatusCode;
                return;
            }

            response.StatusCode = (int)result.StatusCode;
            response.ContentType = result.ContentType;
            switch (result.ResultType)
            {
                case SmtActionResult.ActionResultType.Object:
                    switch (responseType)
                    {
                        case "protobuf":
                            response.Headers["Content-Encoding"] = "gzip";
                            response.ContentType = "application/octet-stream";
                            SmtSettings.Instance.BinarySerializer.Serialize(response.Body, result.ResultValue);
                            break;
                        case "json":
                            response.Headers["Content-Encoding"] = "gzip";
                            response.ContentType = "application/json";
                            SmtSettings.Instance.JsonSerializer.Serialize(response.Body, result.ResultValue);
                            break;
                    }
                    break;
                case SmtActionResult.ActionResultType.Status:
                    response.ContentLength = 0;
                    break;
                case SmtActionResult.ActionResultType.Stream:
                    using (var stream = result.ResultValue as Stream)
                    {
                        response.ContentLength = stream.Length;
                        await stream.CopyToAsync(response.Body);
                    }
                    break;
                case SmtActionResult.ActionResultType.File:
                    var contentDisposition = new ContentDispositionHeaderValue("attachment");
                    contentDisposition.SetHttpFileName(result.FileName);
                    response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
                    using (var stream = result.ResultValue as Stream)
                    {
                        response.ContentLength = stream.Length;
                        await stream.CopyToAsync(response.Body);
                    }
                    break;
            }
        }

        private bool CheckTokenValid(SmtTokenModel token, HttpContext context)
        {
            var dbContext = (ContextType)context.RequestServices.GetService(typeof(ContextType));
            if (token.UserId == 0)
            {
                if (dbContext.SmtTenant.Any(p => p.ID == token.TenantId && p.TokenValidTime <= token.CreateTime) == false)
                {
                    return false;
                }
            }
            else
            {
                if (dbContext.SmtUser.Any(p => p.ID == token.UserId && p.TokenValidTime <= token.CreateTime) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckActionPermission(string controllerName, string actionName, SmtTokenModel token)
        {
            if (token.Claims.ContainsKey("*") == true)
            {
                return true;
            }

            List<string> actions;
            if (token.Claims.TryGetValue(controllerName, out actions) == true)
            {
                if (actions.Contains(actionName) == true || actions.Contains("*") == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
