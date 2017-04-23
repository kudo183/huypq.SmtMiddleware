using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using huypq.SmtMiddleware.Constant;

namespace huypq.SmtMiddleware
{
    public abstract class SmtBaseController<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType> : SmtAbstractController, IDisposable
        where TenantEntityType : class, SmtITenant, new()
        where UserEntityType : class, SmtIUser, new()
        where UserClaimEntityType : class, SmtIUserClaim
        where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
    {
        private ContextType _context;

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
                case ActionName.Register:
                    result = Register(parameter["user"].ToString(), parameter["pass"].ToString(), parameter["tenantname"].ToString());
                    break;
                case ActionName.TenantLogin:
                    result = TenantLogin(parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case ActionName.UserLogin:
                    result = UserLogin(parameter["tenant"].ToString(), parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case ActionName.ResetUserPassword:
                    result = ResetUserPassword(parameter["user"].ToString());
                    break;
                case ActionName.ChangePassword:
                    result = ChangePassword(parameter["currentpass"].ToString(), parameter["newpass"].ToString());
                    break;
                case ActionName.TenantRequestToken:
                    result = TenantRequestToken(parameter["email"].ToString(), parameter["purpose"].ToString());
                    break;
                case ActionName.UserRequestToken:
                    result = UserRequestToken(parameter["email"].ToString(), parameter["tenantname"].ToString(), parameter["purpose"].ToString());
                    break;
                case ActionName.ResetPassword:
                    result = ResetPassword(parameter["token"].ToString(), parameter["pass"].ToString());
                    break;
                case ActionName.ConfirmEmail:
                    result = ConfirmEmail(parameter["token"].ToString());
                    break;
                case ActionName.Logout:
                    result = Logout();
                    break;
                case "ip":
                    result = CreateObjectResult(GetIPAddress());
                    break;
                default:
                    break;
            }

            return result;
        }

        public SmtActionResult Register(string user, string pass, string tenantName)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
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
                    return CreateStatusResult(System.Net.HttpStatusCode.BadRequest, "Username not available");
                case 2:
                    return CreateStatusResult(System.Net.HttpStatusCode.BadRequest, "Tenant name not available");
                case 3:
                    return CreateStatusResult(System.Net.HttpStatusCode.BadRequest, "Username and Tenant name not available");
            }

            var entity = new TenantEntityType()
            {
                Email = user,
                PasswordHash = Crypto.PasswordHash.HashedBase64String(pass),
                CreateDate = DateTime.UtcNow,
                TenantName = tenantName,
                TokenValidTime = DateTime.UtcNow.Ticks
            };
            _context.SmtTenant.Add(entity);

            _context.SaveChanges();

            MailUtils.SendTenantToken(user, TokenPurpose.ConfirmEmail);

            return CreateOKResult();
        }

        public SmtActionResult TenantLogin(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == user);
            if (tenantEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.NotFound);
            }

            if (tenantEntity.IsConfirmed == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized, "NotConfirmed");
            }

            if (tenantEntity.IsLocked == true)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized, "Locked");
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(tenantEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var token = new TokenManager.LoginToken() { TenantName = tenantEntity.TenantName, TenantID = tenantEntity.ID };
            token.Claims.Add("*", new List<string>());
            return CreateObjectResult(TokenManager.LoginToken.CreateTokenString(token));
        }

        public SmtActionResult UserLogin(string tenant, string user, string pass)
        {
            if (string.IsNullOrEmpty(tenant) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == tenant);
            if (tenantEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.NotFound);
            }

            if (tenantEntity.IsLocked == true)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized, "Locked");
            }

            var userEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user && p.TenantID == tenantEntity.ID);
            if (userEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.NotFound);
            }

            if (userEntity.IsLocked == true)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized, "Locked");
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(userEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var token = new TokenManager.LoginToken() { UserName = userEntity.UserName, TenantName = tenantEntity.TenantName, TenantID = userEntity.TenantID, UserID = userEntity.ID };
            foreach (var uc in _context.SmtUserClaim.Where(p => p.UserID == userEntity.ID))
            {
                var temp = uc.Claim.Split('.');
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

        public SmtActionResult ResetUserPassword(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            if (TokenModel.IsTenant == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            SmtILogin loginEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user);

            if (loginEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            loginEntity.PasswordHash = Crypto.PasswordHash.HashedBase64String(SmtSettings.Instance.DefaultUserPassword);
            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }

        public SmtActionResult ChangePassword(string currentPass, string newPass)
        {
            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            SmtILogin loginEntity = null;
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
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(loginEntity.PasswordHash, currentPass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
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
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            var token = TokenManager.Token.VerifyTokenString(tokenString, TokenPurpose.ResetPassword);

            SmtILogin loginEntity = null;
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
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
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
                case TokenPurpose.ConfirmEmail:
                case TokenPurpose.ResetPassword:
                    break;
                default:
                    return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            if (_context.SmtTenant.Any(p => p.Email == email) == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            MailUtils.SendTenantToken(email, purpose);

            return CreateOKResult();
        }

        public SmtActionResult UserRequestToken(string email, string tenantName, string purpose)
        {
            switch (purpose)
            {
                case TokenPurpose.ConfirmEmail:
                case TokenPurpose.ResetPassword:
                    break;
                default:
                    return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == tenantName);
            if (tenantEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            if (_context.SmtUser.Any(p => p.Email == email && p.TenantID == tenantEntity.ID) == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            MailUtils.SendUserToken(email, tenantName, purpose);

            return CreateOKResult();
        }

        public SmtActionResult ConfirmEmail(string tokenString)
        {
            var token = TokenManager.Token.VerifyTokenString(tokenString, TokenPurpose.ConfirmEmail);

            SmtILogin loginEntity = null;

            if (token.IsTenant == true)
            {
                loginEntity = _context.SmtTenant.FirstOrDefault(p => p.Email == token.Email);
            }
            else
            {
                var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.TenantName == token.TenantName);
                loginEntity = _context.SmtUser.FirstOrDefault(p => p.Email == token.Email && p.TenantID == tenantEntity.ID);
            }

            loginEntity.IsConfirmed = true;
            loginEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(loginEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateOKResult();
        }

        public SmtActionResult Logout()
        {
            SmtILogin loginEntity = null;

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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
