using Google.Apis.Auth;
using huypq.SmtShared.Constant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace huypq.SmtMiddleware
{
    public abstract class SmtBaseController<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType> : SmtAbstractController, IDisposable
        where TenantEntityType : class, ITenant, new()
        where UserEntityType : class, IUser, new()
        where UserClaimEntityType : class, IUserClaim
        where ContextType : DbContext, IDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
    {
        private ContextType _context;
        private Regex _emailValidator = new Regex(@"^[a-zA-Z0-9.]+@[a-zA-Z0-9]+\.[a-zA-Z]+");

        public override void Init(TokenManager.LoginToken token, IApplicationBuilder app, HttpContext context, string requestType)
        {
            base.Init(token, app, context, requestType);
            _context = (ContextType)Context.RequestServices.GetService(typeof(ContextType));
        }

        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            SmtActionResult result = null;

            switch (actionName)
            {
                case ControllerAction.Smt.Register:
                    result = Register(parameter["user"].ToString(), parameter["tenantname"].ToString());
                    break;
                case ControllerAction.Smt.TenantLogin:
                    result = TenantLogin(parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case ControllerAction.Smt.TenantLoginWithIdToken:
                    result = TenantLoginWithIdTokenAsync(parameter).Result;
                    break;
                case ControllerAction.Smt.UserLogin:
                    result = UserLogin(parameter["tenant"].ToString(), parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case ControllerAction.Smt.LockUser:
                    result = LockUser(parameter["user"].ToString(), bool.Parse(parameter["islocked"].ToString()));
                    break;
                case ControllerAction.Smt.ChangePassword:
                    result = ChangePassword(parameter["currentpass"].ToString(), parameter["newpass"].ToString());
                    break;
                case ControllerAction.Smt.TenantRequestToken:
                    result = TenantRequestToken(parameter["email"].ToString(), parameter["purpose"].ToString());
                    break;
                case ControllerAction.Smt.UserRequestToken:
                    result = UserRequestToken(parameter["email"].ToString(), parameter["tenantname"].ToString(), parameter["purpose"].ToString());
                    break;
                case ControllerAction.Smt.ResetPassword:
                    result = ResetPassword(parameter["token"].ToString(), parameter["pass"].ToString());
                    break;
                case ControllerAction.Smt.Logout:
                    result = Logout();
                    break;
                case ControllerAction.Smt.IP:
                    result = CreateObjectResult(GetIPAddress());
                    break;
                case ControllerAction.Smt.Ping:
                    result = CreateObjectResult(null);
                    break;
                case ControllerAction.Smt.TenantLogout:
                    result = TenantLogout(parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case ControllerAction.Smt.UserLogout:
                    result = UserLogout(parameter["tenant"].ToString(), parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                default:
                    break;
            }

            return result;
        }

        public SmtActionResult Register(string user, string tenantName)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(tenantName))
            {
                return CreateObjectResult("user/tenantName cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            if (_emailValidator.IsMatch(user) == false)
            {
                return CreateObjectResult("user is not valid email", System.Net.HttpStatusCode.BadRequest);
            }

            var validate = 0;

            if (_context.SmtTenant.Any(p => p.Email == user))
            {
                validate = validate + 1;
            }

            if (_context.SmtTenant.Any(p => p.TenantName == tenantName))
            {
                validate = validate + 2;
            }

            switch (validate)
            {
                case 1:
                    return CreateObjectResult("Username not available", System.Net.HttpStatusCode.Conflict);
                case 2:
                    return CreateObjectResult("Tenant name not available", System.Net.HttpStatusCode.Conflict);
                case 3:
                    return CreateObjectResult("Username and Tenant name not available", System.Net.HttpStatusCode.Conflict);
            }

            var entity = new TenantEntityType()
            {
                Email = user,
                PasswordHash = string.Empty,
                CreateDate = DateTime.UtcNow,
                TenantName = tenantName,
                TokenValidTime = DateTime.UtcNow.Ticks
            };
            _context.SmtTenant.Add(entity);

            _context.SaveChanges();

            MailUtils.SendTenantToken(user, TokenPurpose.ResetPassword);

            return CreateOKResult();
        }

        public SmtActionResult TenantLogin(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateObjectResult("user/pass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == user);
            if (tenantEntity == null)
            {
                return CreateObjectResult(null, System.Net.HttpStatusCode.NotFound);
            }

            if (tenantEntity.IsLocked == true)
            {
                return CreateObjectResult("User is locked", System.Net.HttpStatusCode.BadRequest);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(tenantEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateObjectResult("wrong password", System.Net.HttpStatusCode.BadRequest);
            }

            var token = new TokenManager.LoginToken() { TenantName = tenantEntity.TenantName, TenantID = tenantEntity.ID };
            token.Claims.Add("*", new List<string>());
            return CreateObjectResult(TokenManager.LoginToken.CreateTokenString(token));
        }

        public async System.Threading.Tasks.Task<SmtActionResult> TenantLoginWithIdTokenAsync(Dictionary<string, object> parameter)
        {
            var idToken = parameter["idtoken"].ToString();
            var provider = parameter["provider"].ToString();

            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(provider))
            {
                return CreateObjectResult("idToken/provider cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            string user = "";
            switch (provider)
            {
                case "google":
                    {
                        var validPayload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                        if (validPayload.Audience.ToString() != SmtSettings.Instance.GoogleClientID)
                        {
                            return CreateObjectResult("invalid token", System.Net.HttpStatusCode.BadRequest);
                        }
                        user = validPayload.Email;
                    }
                    break;
                case "facebook":
                    {
                        using (var algorithm = new HMACSHA256(Encoding.ASCII.GetBytes(SmtSettings.Instance.FacebookAppSecret)))
                        {
                            var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(idToken));
                            var builder = new StringBuilder();
                            for (int i = 0; i < hash.Length; i++)
                            {
                                builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                            }
                            var appsecret_proof = builder.ToString();
                            using (HttpClient client = new HttpClient())
                            {
                                var response = await client.GetAsync(SmtSettings.Instance.FacebookUserInfoEndPoint + "?fields=email&access_token=" + idToken + "&appsecret_proof=" + appsecret_proof);
                                if (response.IsSuccessStatusCode == true)
                                {
                                    user = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["email"];
                                }
                            }
                        }
                    }
                    break;
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == user);
            if (tenantEntity == null)
            {
                tenantEntity = new TenantEntityType()
                {
                    Email = user,
                    PasswordHash = string.Empty,
                    CreateDate = DateTime.UtcNow,
                    TenantName = user,
                    TokenValidTime = DateTime.UtcNow.Ticks
                };
                _context.SmtTenant.Add(tenantEntity);

                _context.SaveChanges();
            }

            if (tenantEntity.IsLocked == true)
            {
                return CreateObjectResult("User is locked", System.Net.HttpStatusCode.BadRequest);
            }

            var token = new TokenManager.LoginToken() { TenantName = tenantEntity.TenantName, TenantID = tenantEntity.ID };
            token.Claims.Add("*", new List<string>());
            return CreateObjectResult(TokenManager.LoginToken.CreateTokenString(token));
        }

        public SmtActionResult UserLogin(string tenant, string user, string pass)
        {
            if (string.IsNullOrEmpty(tenant) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateObjectResult("user/tenantName/pass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == tenant);
            if (tenantEntity == null)
            {
                return CreateObjectResult("Tenant not found", System.Net.HttpStatusCode.NotFound);
            }

            if (tenantEntity.IsLocked == true)
            {
                return CreateObjectResult("User is locked", System.Net.HttpStatusCode.BadRequest);
            }

            var userEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user && p.TenantID == tenantEntity.ID);
            if (userEntity == null)
            {
                return CreateObjectResult("user not found", System.Net.HttpStatusCode.NotFound);
            }

            if (userEntity.IsLocked == true)
            {
                return CreateObjectResult("User is locked", System.Net.HttpStatusCode.BadRequest);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(userEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateObjectResult("wrong pass", System.Net.HttpStatusCode.BadRequest);
            }

            var token = new TokenManager.LoginToken() { UserName = userEntity.UserName, TenantName = tenantEntity.TenantName, TenantID = userEntity.TenantID, UserID = userEntity.ID };
            foreach (var uc in _context.SmtUserClaim.Where(p => p.UserID == userEntity.ID))
            {
                var temp = uc.Claim.Split('.');
                if (temp.Length != 2)
                {
                    continue;
                }
                var controllerName = temp[0];
                var actionName = temp[1];
                List<string> actions;
                if (token.Claims.TryGetValue(controllerName, out actions) == false)
                {
                    actions = new List<string>();
                    token.Claims.Add(controllerName, actions);
                }
                actions.Add(actionName);
            }

            return CreateObjectResult(TokenManager.LoginToken.CreateTokenString(token));
        }

        public SmtActionResult LockUser(string user, bool isLocked)
        {
            if (string.IsNullOrEmpty(user))
            {
                return CreateObjectResult("user cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            if (TokenModel.IsTenant == false)
            {
                return CreateObjectResult("only Tenant is allowed", System.Net.HttpStatusCode.BadRequest);
            }

            ILogin loginEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user);

            if (loginEntity == null)
            {
                return CreateObjectResult("user not found", System.Net.HttpStatusCode.NotFound);
            }

            loginEntity.IsLocked = isLocked;
            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }

        public SmtActionResult ChangePassword(string currentPass, string newPass)
        {
            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass))
            {
                return CreateObjectResult("currentPass/newPass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            ILogin loginEntity = null;
            if (TokenModel.IsTenant == true)
            {
                loginEntity = _context.SmtTenant.FirstOrDefault(p => p.ID == TokenModel.TenantID);
            }
            else
            {
                loginEntity = _context.SmtUser.FirstOrDefault(p => p.ID == TokenModel.UserID);
            }
            if (loginEntity == null)
            {
                return CreateObjectResult("user not found", System.Net.HttpStatusCode.NotFound);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(loginEntity.PasswordHash, currentPass);
            if (result == false)
            {
                return CreateObjectResult("wrong pass", System.Net.HttpStatusCode.BadRequest);
            }

            loginEntity.PasswordHash = Crypto.PasswordHash.HashedBase64String(newPass);
            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }

        public SmtActionResult ResetPassword(string tokenString, string newPass)
        {
            if (string.IsNullOrEmpty(newPass))
            {
                return CreateObjectResult("newPass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            var token = TokenManager.Token.VerifyTokenString(tokenString, TokenPurpose.ResetPassword);

            if (token.IsExpired() == true)
            {
                return CreateObjectResult("token expired", System.Net.HttpStatusCode.BadRequest);
            }

            ILogin loginEntity = null;
            if (token.IsTenant == true)
            {
                loginEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == token.Email);
            }
            else
            {
                var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == token.TenantName);
                loginEntity = _context.SmtUser.FirstOrDefault(p => p.Email == token.Email && p.TenantID == tenantEntity.ID);
            }
            if (loginEntity == null)
            {
                return CreateObjectResult("user not found", System.Net.HttpStatusCode.NotFound);
            }

            loginEntity.PasswordHash = Crypto.PasswordHash.HashedBase64String(newPass);
            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }

        public SmtActionResult TenantRequestToken(string email, string purpose)
        {
            switch (purpose)
            {
                case TokenPurpose.ResetPassword:
                    break;
                default:
                    return CreateObjectResult("wrong token purpose", System.Net.HttpStatusCode.BadRequest);
            }

            if (_context.SmtTenant.Any(p => p.Email == email) == false)
            {
                return CreateObjectResult("email not found", System.Net.HttpStatusCode.NotFound);
            }

            MailUtils.SendTenantToken(email, purpose);

            return CreateOKResult();
        }

        public SmtActionResult UserRequestToken(string email, string tenantName, string purpose)
        {
            switch (purpose)
            {
                case TokenPurpose.ResetPassword:
                    break;
                default:
                    return CreateObjectResult("wrong token purpose", System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == tenantName);
            if (tenantEntity == null)
            {
                return CreateObjectResult("tenantName not found", System.Net.HttpStatusCode.NotFound);
            }

            if (_context.SmtUser.Any(p => p.Email == email && p.TenantID == tenantEntity.ID) == false)
            {
                return CreateObjectResult("email not found", System.Net.HttpStatusCode.NotFound);
            }

            MailUtils.SendUserToken(email, tenantName, purpose);

            return CreateOKResult();
        }

        public SmtActionResult Logout()
        {
            ILogin loginEntity = null;

            if (TokenModel.IsTenant == true)
            {
                loginEntity = _context.SmtTenant.FirstOrDefault(p => p.ID == TokenModel.TenantID);
            }
            else
            {
                loginEntity = _context.SmtUser.FirstOrDefault(p => p.ID == TokenModel.UserID);
            }

            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }


        public SmtActionResult TenantLogout(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateObjectResult("user/pass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == user);
            if (tenantEntity == null)
            {
                return CreateObjectResult(null, System.Net.HttpStatusCode.NotFound);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(tenantEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateObjectResult("wrong password", System.Net.HttpStatusCode.BadRequest);
            }

            tenantEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(tenantEntity).State = EntityState.Modified;

            _context.SaveChanges();
            return CreateOKResult();
        }

        public SmtActionResult UserLogout(string tenant, string user, string pass)
        {
            if (string.IsNullOrEmpty(tenant) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateObjectResult("user/tenantName/pass cannot empty", System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == tenant);
            if (tenantEntity == null)
            {
                return CreateObjectResult("Tenant not found", System.Net.HttpStatusCode.NotFound);
            }

            var userEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user && p.TenantID == tenantEntity.ID);
            if (userEntity == null)
            {
                return CreateObjectResult("user not found", System.Net.HttpStatusCode.NotFound);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(userEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateObjectResult("wrong pass", System.Net.HttpStatusCode.BadRequest);
            }

            userEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(userEntity).State = EntityState.Modified;

            _context.SaveChanges();
            return CreateOKResult();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
