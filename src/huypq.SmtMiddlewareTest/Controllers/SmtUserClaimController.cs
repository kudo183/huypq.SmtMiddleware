using huypq.SmtMiddleware;
using huypq.SmtSharedTest;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class SmtUserClaimController : SmtUserClaimBaseController<TestContext, SmtUserClaim, SmtUserClaimDto, SmtTenant, SmtUser>
    {
        public override string GetControllerName()
        {
            return nameof(SmtUserClaimController);
        }
    }
}
