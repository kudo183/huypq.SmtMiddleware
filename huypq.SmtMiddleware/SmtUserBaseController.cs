using System.Collections.Generic;
using huypq.SmtShared;
using huypq.SmtShared.Constant;
using Microsoft.EntityFrameworkCore;

namespace huypq.SmtMiddleware
{
    public abstract class SmtUserBaseController<ContextType, EntityType, DtoType> : SmtEntityBaseController<ContextType, EntityType, DtoType>
        where ContextType : DbContext, IDbContext
        where EntityType : class, IUser, new()
        where DtoType : class, IUserDto, new()
    {
        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            if (TokenModel.IsTenant == false)
            {
                return CreateObjectResult("only Tenant is allowed", System.Net.HttpStatusCode.Unauthorized);
            }

            return base.ActionInvoker(actionName, parameter);
        }

        public override DtoType ConvertToDto(EntityType entity)
        {
            var dto = new DtoType()
            {
                CreateDate = entity.CreateDate,
                Email = entity.Email,
                UserName = entity.UserName,
                ID = entity.ID,
                IsLocked = entity.IsLocked,
                LastUpdateTime = entity.LastUpdateTime,
                PasswordHash = entity.PasswordHash,
                TenantID = entity.TenantID,
                TokenValidTime = entity.TokenValidTime
            };
            return dto;
        }

        public override EntityType ConvertToEntity(DtoType dto)
        {
            var entity = new EntityType()
            {
                CreateDate = dto.CreateDate,
                Email = dto.Email,
                UserName = dto.UserName,
                ID = dto.ID,
                PasswordHash = dto.PasswordHash,
                TenantID = dto.TenantID,
                TokenValidTime = dto.TokenValidTime
            };

            var now = System.DateTime.UtcNow;
            if (dto.State == DtoState.Add)
            {
                entity.PasswordHash = string.Empty;
                entity.CreateDate = now;
                entity.TokenValidTime = now.Ticks;

                MailUtils.SendUserToken(entity.Email, TokenModel.TenantName, TokenPurpose.ResetPassword);
            }
            return entity;
        }
    }
}
