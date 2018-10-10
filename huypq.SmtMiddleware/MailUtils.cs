using System;
using System.IO;

namespace huypq.SmtMiddleware
{
    public static class MailUtils
    {
        public static void SendTenantToken(string email, string purpose)
        {
            var token = new TokenManager.Token()
            {
                IsTenant = true,
                Email = email,
                Purpose = purpose,
                ExpireTime = DateTime.UtcNow.AddMinutes(60)
            };
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("tenant");
            sb.AppendLine(string.Format("$user\t{0}", email));
            sb.AppendLine(string.Format("$purpose\t{0}", purpose));
            sb.AppendLine(string.Format("$token\t{0}", TokenManager.Token.CreateTokenString(token)));
            var content = sb.ToString();
            if (Directory.Exists(SmtSettings.Instance.EmailFolderPath) == false)
            {
                Directory.CreateDirectory(SmtSettings.Instance.EmailFolderPath);
            }

            File.WriteAllText(Path.Combine(SmtSettings.Instance.EmailFolderPath, string.Format("{0}.txt", DateTime.UtcNow.Ticks)), content);
        }

        public static void SendUserToken(string email, string tenantName, string purpose)
        {
            var token = new TokenManager.Token()
            {
                IsTenant = false,
                Email = email,
                TenantName = tenantName,
                Purpose = purpose,
                ExpireTime = DateTime.UtcNow.AddMinutes(60)
            };
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("user");
            sb.AppendLine(string.Format("$user\t{0}", email));
            sb.AppendLine(string.Format("$tenant\t{0}", tenantName));
            sb.AppendLine(string.Format("$purpose\t{0}", purpose));
            sb.AppendLine(string.Format("$token\t{0}", TokenManager.Token.CreateTokenString(token)));
            var content = sb.ToString();
            if (Directory.Exists(SmtSettings.Instance.EmailFolderPath) == false)
            {
                Directory.CreateDirectory(SmtSettings.Instance.EmailFolderPath);
            }

            File.WriteAllText(Path.Combine(SmtSettings.Instance.EmailFolderPath, string.Format("{0}.txt", DateTime.UtcNow.Ticks)), content);
        }
    }
}
