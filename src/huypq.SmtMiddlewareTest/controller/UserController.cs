using huypq.SmtMiddleware;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class UserController : SmtEntityBaseController<TestContext, SmtUser, UserDto>
    {
        public override UserDto ConvertToDto(SmtUser entity)
        {
            var dto = new UserDto();
            dto.CreateDate = entity.CreateDate;
            dto.Email = entity.Email;
            dto.UserName = entity.UserName;
            dto.ID = entity.ID;
            dto.TenantID = entity.TenantID;
            return dto;
        }

        public override SmtUser ConvertToEntity(UserDto dto)
        {
            var entity = new SmtUser();
            entity.CreateDate = dto.CreateDate;
            entity.Email = dto.Email;
            entity.UserName = dto.UserName;
            entity.ID = dto.ID;
            entity.TenantID = dto.TenantID;
            return entity;
        }

        public override string GetControllerName()
        {
            return nameof(UserController);
        }
    }
}
