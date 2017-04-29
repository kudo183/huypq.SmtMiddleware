using huypq.SmtMiddleware;
using huypq.SmtShared.Constant;
using huypq.SmtSharedTest;
using QueryBuilder;
using System.Collections.Generic;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class TestDataController : SmtEntityBaseController<TestContext, TestData, TestDataDto>
    {
        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            switch (actionName)
            {
                case ControllerAction.SmtEntityBase.GetAll:
                    return GetAll(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
                case ControllerAction.SmtEntityBase.GetUpdate:
                    return GetUpdate(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
            }
            return base.ActionInvoker(actionName, parameter);
        }

        public override TestDataDto ConvertToDto(TestData entity)
        {
            return new TestDataDto()
            {
                ID = entity.ID,
                TenantID = entity.TenantID,
                Data = entity.Data,
                LastUpdateTime = entity.LastUpdateTime,
                CreateTime=entity.CreateTime
            };
        }

        public override TestData ConvertToEntity(TestDataDto dto)
        {
            return new TestData()
            {
                ID = dto.ID,
                TenantID = dto.TenantID,
                Data = dto.Data,
                LastUpdateTime = dto.LastUpdateTime,
                CreateTime = dto.CreateTime
            };
        }

        public override string GetControllerName()
        {
            return nameof(TestDataController);
        }
    }
}
