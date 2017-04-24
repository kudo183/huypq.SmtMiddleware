using System.Collections.Generic;
using huypq.SmtShared;
using Microsoft.EntityFrameworkCore;

namespace huypq.SmtMiddleware
{
    public abstract class SmtUserBaseController<ContextType, EntityType, DtoType> : SmtEntityBaseController<ContextType, EntityType, DtoType>
        where ContextType : DbContext
        where EntityType : class, SmtIUser, new()
        where DtoType : class, SmtIUserDto, new()
    {
        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            if (TokenModel.IsTenant == false)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
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
                IsConfirmed = entity.IsConfirmed,
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
                entity.PasswordHash = Crypto.PasswordHash.HashedBase64String(SmtSettings.Instance.DefaultUserPassword);
                entity.CreateDate = now;
                entity.TokenValidTime = now.Ticks;

                MailUtils.SendUserToken(entity.Email, TokenModel.TenantName, Constant.TokenPurpose.ConfirmEmail);
            }
            return entity;
        }
    }
}
