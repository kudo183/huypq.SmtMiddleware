using huypq.SmtMiddleware;
using huypq.SmtSharedTest;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class TestChildDataController : SmtEntityBaseController<TestContext, TestChildData, TestChildDataDto>
    {        
        public override TestChildDataDto ConvertToDto(TestChildData entity)
        {
            return new TestChildDataDto()
            {
                ID = entity.ID,
                TenantID = entity.TenantID,
                Data = entity.Data,
                LastUpdateTime = entity.LastUpdateTime,
                CreateTime = entity.CreateTime,
                TestDataID = entity.TestDataID
            };
        }

        public override TestChildData ConvertToEntity(TestChildDataDto dto)
        {
            return new TestChildData()
            {
                ID = dto.ID,
                TenantID = dto.TenantID,
                Data = dto.Data,
                LastUpdateTime = dto.LastUpdateTime,
                CreateTime = dto.CreateTime,
                TestDataID = dto.TestDataID
            };
        }

        public override string GetControllerName()
        {
            return nameof(TestChildDataController);
        }
    }
}
