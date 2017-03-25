namespace huypq.SmtMiddlewareTest.Controllers
{
    public class SmtController : SmtMiddleware.SmtBaseController<TestContext, SmtTenant, SmtUser, SmtUserClaim>
    {
        public override string GetControllerName()
        {
            return nameof(SmtController);
        }
    }
}
