namespace huypq.SmtMiddleware.Constant
{
    public class ActionName
    {
        public const string Register = "register";
        public const string TenantLogin = "tenantlogin";
        public const string UserLogin = "userlogin";
        public const string TenantRequestToken = "tenantrequesttoken";
        public const string UserRequestToken = "userrequesttoken";
        public const string LockUser = "lockuser";
        public const string ChangePassword = "changepassword";
        public const string ResetPassword = "resetpassword";
        public const string Logout = "logout";
    }

    public class TokenPurpose
    {
        public const string ResetPassword = "resetpassword";
    }
}
