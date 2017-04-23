﻿namespace huypq.SmtMiddleware.Constant
{
    public class ActionName
    {
        public const string Register = "register";
        public const string TenantLogin = "tenantlogin";
        public const string UserLogin = "userlogin";
        public const string TenantRequestToken = "tenantrequesttoken";
        public const string UserRequestToken = "userrequesttoken";
        public const string ResetUserPassword = "resetuserpassword";
        public const string ChangePassword = "changepassword";
        public const string ResetPassword = "resetpassword";
        public const string ConfirmEmail = "confirmemail";
        public const string Logout = "logout";
    }

    public class TokenPurpose
    {
        public const string ConfirmEmail = "confirmemail";
        public const string ResetPassword = "resetpassword";
    }
}
