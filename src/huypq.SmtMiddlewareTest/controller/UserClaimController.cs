using huypq.SmtMiddleware;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class UserClaimController : SmtEntityBaseController<TestContext, SmtUserClaim, UserClaimDto>
    {
        public override UserClaimDto ConvertToDto(SmtUserClaim entity)
        {
            var dto = new UserClaimDto();
            dto.UserID = entity.UserID;
            dto.Claim = entity.Claim;
            dto.ID = entity.ID;
            dto.TenantID = entity.TenantID;
            return dto;
        }

        public override SmtUserClaim ConvertToEntity(UserClaimDto dto)
        {
            var entity = new SmtUserClaim();
            entity.UserID = dto.UserID;
            entity.Claim = dto.Claim;
            entity.ID = dto.ID;
            entity.TenantID = dto.TenantID;
            return entity;
        }

        public override string GetControllerName()
        {
            return nameof(UserClaimController);
        }
    }
}
