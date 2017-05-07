using huypq.SmtMiddleware;
using huypq.SmtShared.Test;

namespace huypq.SmtMiddleware.Test.Controllers
{
    public class SmtUserController : SmtUserBaseController<TestContext, SmtUser, SmtUserDto>
    {
        public override string GetControllerName()
        {
            return nameof(SmtUserController);
        }
    }
}
