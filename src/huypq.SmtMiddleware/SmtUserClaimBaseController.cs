using huypq.SmtShared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace huypq.SmtMiddleware
{
    public abstract class SmtUserClaimBaseController<ContextType, UserClaimEntityType, UserClaimDtoType, TenantEntityType, UserEntityType> : SmtEntityBaseController<ContextType, UserClaimEntityType, UserClaimDtoType>
        where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
        where UserClaimEntityType : class, SmtIUserClaim, new()
        where UserClaimDtoType : class, SmtIUserClaimDto, new()
        where TenantEntityType : class, SmtITenant, new()
        where UserEntityType : class, SmtIUser, new()
    {
        public override UserClaimDtoType ConvertToDto(UserClaimEntityType entity)
        {
            var dto = new UserClaimDtoType()
            {
                UserID = entity.UserID,
                Claim = entity.Claim,
                ID = entity.ID,
                TenantID = entity.TenantID,
                LastUpdateTime = entity.LastUpdateTime
            };

            return dto;
        }

        public override UserClaimEntityType ConvertToEntity(UserClaimDtoType dto)
        {
            var entity = new UserClaimEntityType()
            {
                UserID = dto.UserID,
                Claim = dto.Claim,
                ID = dto.ID,
                TenantID = dto.TenantID,
                LastUpdateTime = dto.LastUpdateTime
            };

            return entity;
        }

        protected override void AfterSave(List<UserClaimDtoType> items, List<UserClaimEntityType> changedEntities)
        {
            var now = DateTime.UtcNow.Ticks;
            foreach (var userId in changedEntities.Select(p => p.UserID).Distinct())
            {
                var user = DBContext.SmtUser.FirstOrDefault(p => p.ID == userId);
                user.TokenValidTime = now;
                DBContext.Entry(user).State = EntityState.Modified;
            }
            DBContext.SaveChanges();
        }
    }
}
