namespace huypq.SmtMiddleware.Test.Controllers
{
    public class SmtController : SmtMiddleware.SmtBaseController<TestContext, SmtTenant, SmtUser, SmtUserClaim>
    {
        public override string GetControllerName()
        {
            return nameof(SmtController);
        }
    }
}
