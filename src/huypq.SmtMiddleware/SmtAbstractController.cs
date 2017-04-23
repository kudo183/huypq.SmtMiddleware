using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;

namespace huypq.SmtMiddleware
{
    public abstract class SmtAbstractController
    {
        /// <summary>
        /// request paramter type: json or protobuf
        /// </summary>
        public string RequestObjectType { get; set; }

        /// <summary>
        /// authentication token
        /// </summary>
        public TokenManager.LoginToken TokenModel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IApplicationBuilder App { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public HttpContext Context { get; set; }

        /// <summary>
        /// mapping actionName to corresponding controller method, including paremeter convert.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public abstract SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter);

        public abstract string GetControllerName();

        public virtual void Init(TokenManager.LoginToken token, IApplicationBuilder app, HttpContext context, string requestType)
        {
            TokenModel = token;
            App = app;
            Context = context;
            RequestObjectType = requestType;
        }

        protected string GetIPAddress()
        {
            var address = Context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature>()?.RemoteIpAddress;
            return address.ToString();
        }

        /// <summary>
        /// response body is binary stream
        /// </summary>
        /// <param name="resultValue"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        protected SmtActionResult CreateStreamResult(
            Stream resultValue,
            string mimeType)
        {
            var result = new SmtActionResult();
            result.ResultType = SmtActionResult.ActionResultType.Stream;
            result.ResultValue = resultValue;
            result.ContentType = mimeType;
            result.StatusCode = System.Net.HttpStatusCode.OK;
            return result;
        }

        /// <summary>
        /// response body is empty
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        protected SmtActionResult CreateStatusResult(
            System.Net.HttpStatusCode statusCode, string msg = null)
        {
            var result = new SmtActionResult();
            result.ResultType = SmtActionResult.ActionResultType.Status;
            result.ResultValue = msg;
            result.StatusCode = statusCode;
            return result;
        }

        /// <summary>
        /// response body is binary stream
        /// </summary>
        /// <param name="resultValue"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected SmtActionResult CreateFileResult(
            Stream resultValue,
            string fileName)
        {
            var result = new SmtActionResult();
            result.ResultType = SmtActionResult.ActionResultType.File;
            result.ResultValue = resultValue;
            result.ContentType = MimeMapping.GetMimeType(fileName);
            result.FileName = fileName;
            result.StatusCode = System.Net.HttpStatusCode.OK;
            return result;
        }

        /// <summary>
        /// response type is chosen by client request Headers["response"]:
        ///     "json" -> json text
        ///     "protobuf" -> protobuf binary
        /// </summary>
        /// <param name="resultValue"></param>
        /// <returns></returns>
        protected SmtActionResult CreateObjectResult(
            object resultValue)
        {
            var result = new SmtActionResult();
            result.ResultType = SmtActionResult.ActionResultType.Object;
            result.ResultValue = resultValue;
            result.StatusCode = System.Net.HttpStatusCode.OK;
            return result;
        }

        /// <summary>
        /// CreateObjectResult("OK")
        /// </summary>
        /// <returns></returns>
        protected SmtActionResult CreateOKResult()
        {
            return CreateObjectResult("OK");
        }
    }
}
