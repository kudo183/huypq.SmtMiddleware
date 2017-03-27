using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace huypq.SmtMiddleware
{
    public abstract class SmtBaseController<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType> : SmtAbstractController, IDisposable
        where TenantEntityType : class, SmtITenant, new()
        where UserEntityType : class, SmtIUser, new()
            where UserClaimEntityType : class, SmtIUserClaim
            where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
    {
        private ContextType _context;

        public override void Init(SmtTokenModel token, IApplicationBuilder app, HttpContext context, string requestType)
        {
            base.Init(token, app, context, requestType);
            _context = (ContextType)Context.RequestServices.GetService(typeof(ContextType));
        }

        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            SmtActionResult result = null;

            switch (actionName)
            {
                case "register":
                    result = Register(parameter["user"].ToString(), parameter["pass"].ToString(), parameter["tenantname"].ToString());
                    break;
                case "tenantlogin":
                    result = TenantLogin(parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case "tenantlogout":
                    result = TenantLogout();
                    break;
                case "tenantchangepassword":
                    result = TenantChangePassword(parameter["currentpass"].ToString(), parameter["newpass"].ToString());
                    break;
                case "createuser":
                    result = CreateUser(parameter["loginname"].ToString(), parameter["displayname"].ToString(), parameter["pass"].ToString());
                    break;
                case "userlogin":
                    result = UserLogin(parameter["tenant"].ToString(), parameter["user"].ToString(), parameter["pass"].ToString());
                    break;
                case "userlogout":
                    result = UserLogout();
                    break;
                case "userchangepassword":
                    result = UserChangePassword(parameter["currentpass"].ToString(), parameter["newpass"].ToString());
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
                    return CreateObjectResult("Username not available");
                case 2:
                    return CreateObjectResult("Tenant name not available");
                case 3:
                    return CreateObjectResult("Username and Tenant name not available");
            }

            var hasher = new Crypto.PasswordHash();
            var entity = new TenantEntityType()
            {
                Email = user,
                PasswordHash = hasher.HashedBase64String(pass),
                CreateDate = DateTime.UtcNow,
                TenantName = tenantName,
                TokenValidTime = DateTime.UtcNow.Ticks
            };
            _context.SmtTenant.Add(entity);

            _context.SaveChanges();

            return CreateObjectResult("OK");
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
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(tenantEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var token = new SmtTokenModel() { Name = tenantEntity.Email, TenantId = tenantEntity.ID };
            token.Claims.Add("*", new List<string>());
            return CreateObjectResult(token);
        }

        public SmtActionResult TenantLogout()
        {
            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.ID == TokenModel.TenantId);

            tenantEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(tenantEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateObjectResult("OK");
        }

        public SmtActionResult TenantChangePassword(string currentPass, string newPass)
        {
            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass) || TokenModel.UserId != 0)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            var tenantEntity = _context.SmtTenant.FirstOrDefault(p => p.ID == TokenModel.TenantId);
            if (tenantEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(tenantEntity.PasswordHash, currentPass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var hasher = new Crypto.PasswordHash();
            tenantEntity.PasswordHash = hasher.HashedBase64String(newPass);
            tenantEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(tenantEntity).State = EntityState.Modified;

            _context.SaveChanges();
            
            return CreateObjectResult("OK");
        }

        public SmtActionResult CreateUser(string loginName, string displayName, string pass)
        {
            if (string.IsNullOrEmpty(loginName) || string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(pass))
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            if (TokenModel.TenantId == 0)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            if (_context.SmtUser.Any(p => p.Email == loginName && p.TenantID == TokenModel.TenantId))
            {
                return CreateObjectResult("Username not available");
            }

            var hasher = new Crypto.PasswordHash();
            var entity = new UserEntityType()
            {
                Email = loginName,
                PasswordHash = hasher.HashedBase64String(pass),
                CreateDate = DateTime.UtcNow,
                TenantID = TokenModel.TenantId,
                UserName = displayName,
                TokenValidTime = DateTime.UtcNow.Ticks
            };
            _context.SmtUser.Add(entity);

            _context.SaveChanges();

            return CreateObjectResult("OK");
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
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var userEntity = _context.SmtUser.FirstOrDefault(p => p.Email == user && p.TenantID == tenantEntity.ID);
            if (userEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(userEntity.PasswordHash, pass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var token = new SmtTokenModel() { Name = userEntity.Email, TenantId = userEntity.TenantID, UserId = userEntity.ID };
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

            return CreateObjectResult(token);
        }

        public SmtActionResult UserLogout()
        {
            var userEntity = _context.SmtUser.FirstOrDefault(p => p.ID == TokenModel.UserId);

            userEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(userEntity).State = EntityState.Modified;

            _context.SaveChanges();

            return CreateObjectResult("OK");
        }

        public SmtActionResult UserChangePassword(string currentPass, string newPass)
        {
            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass) || TokenModel.UserId == 0)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.BadRequest);
            }

            var userEntity = _context.SmtUser.FirstOrDefault(p => p.ID == TokenModel.UserId);
            if (userEntity == null)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var result = Crypto.PasswordHash.VerifyHashedPassword(userEntity.PasswordHash, currentPass);
            if (result == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            var hasher = new Crypto.PasswordHash();
            userEntity.PasswordHash = hasher.HashedBase64String(newPass);
            userEntity.TokenValidTime = DateTime.UtcNow.Ticks;
            _context.Entry(userEntity).State = EntityState.Modified;

            _context.SaveChanges();
                        
            return CreateObjectResult("OK");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
