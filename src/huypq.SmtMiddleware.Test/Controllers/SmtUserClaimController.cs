using huypq.SmtMiddleware;
using huypq.SmtShared.Test;

namespace huypq.SmtMiddleware.Test.Controllers
{
    public class SmtUserClaimController : SmtUserClaimBaseController<TestContext, SmtUserClaim, SmtUserClaimDto, SmtTenant, SmtUser>
    {
        public override string GetControllerName()
        {
            return nameof(SmtUserClaimController);
        }
    }
}
