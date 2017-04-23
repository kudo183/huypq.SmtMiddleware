using huypq.SmtMiddleware;
using huypq.SmtSharedTest;

namespace huypq.SmtMiddlewareTest.Controllers
{
    public class SmtUserController : SmtUserBaseController<TestContext, SmtUser, SmtUserDto>
    {
        public override string GetControllerName()
        {
            return nameof(SmtUserController);
        }
    }
}
