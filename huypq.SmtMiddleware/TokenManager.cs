using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware
{
    public static class TokenManager
    {
        public static IServiceProvider ServiceProvider;

        public class Token
        {
            public string Purpose { get; set; }
            public bool IsTenant { get; set; }
            public string Email { get; set; }
            public string TenantName { get; set; }
            public long CreateTime { get; set; }
            public DateTime ExpireTime { get; set; }
            public byte[] CustomData { get; set; }
            public int TenantID { get; set; }

            public Token()
            {
                CreateTime = DateTime.UtcNow.Ticks;
                Email = TenantName = string.Empty;
                CustomData = new byte[0];
            }

            public bool IsExpired()
            {
                return ExpireTime < DateTime.UtcNow;
            }

            public static string CreateTokenString(Token token)
            {
                var protector = GetProtector(token.Purpose);
                if (token.ExpireTime == null)
                {
                    token.ExpireTime = DateTime.UtcNow.AddMinutes(30);
                }
                return protector.Protect(token.ToBase64());
            }

            public static Token VerifyTokenString(string token, string purpose)
            {
                try
                {
                    var protector = GetProtector(purpose);
                    var base64PlainToken = protector.Unprotect(token);

                    return FromBase64(base64PlainToken);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            private string ToBase64()
            {
                using (var ms = new System.IO.MemoryStream())
                using (var bw = new System.IO.BinaryWriter(ms))
                {
                    bw.Write(CreateTime);
                    bw.Write(ExpireTime.Ticks);
                    bw.Write(IsTenant);
                    bw.Write(Email);
                    bw.Write(TenantName);
                    bw.Write(CustomData.Length);
                    bw.Write(CustomData);
                    bw.Write(TenantID);
                    bw.Flush();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }

            private static Token FromBase64(string str)
            {
                var result = new Token();

                using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(str)))
                using (var br = new System.IO.BinaryReader(ms))
                {
                    result.CreateTime = br.ReadInt64();
                    result.ExpireTime = new DateTime(br.ReadInt64());
                    result.IsTenant = br.ReadBoolean();
                    result.Email = br.ReadString();
                    result.TenantName = br.ReadString();
                    result.CustomData = br.ReadBytes(br.ReadInt32());
                    result.TenantID = br.ReadInt32();
                    return result;
                }
            }
        }

        public class LoginToken
        {
            private const string Purpose = "LoginToken";
            /// <summary>
            /// Tenant Name
            /// </summary>
            public string TenantName { get; set; }

            /// <summary>
            /// User Name
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// Id of the logged in user if login is user
            /// </summary>
            public int UserID { get; set; }

            /// <summary>
            /// TenantId of the tenant if login is tenant
            /// </summary>
            public int TenantID { get; set; }

            public bool IsTenant { get { return UserID == 0; } }

            /// <summary>
            /// Token creation time
            /// </summary>
            public long CreateTime { get; set; }

            /// <summary>
            /// key: controller, value: list of action
            /// </summary>
            public Dictionary<string, List<string>> Claims { get; set; }

            public LoginToken()
            {
                CreateTime = DateTime.UtcNow.Ticks;
                Claims = new Dictionary<string, List<string>>();
                TenantName = UserName = string.Empty;
            }

            public static string CreateTokenString(LoginToken token)
            {
                var protector = GetProtector(Purpose);
                var tokenText = protector.Protect(token.ToBase64());

                return protector.Protect(token.ToBase64());
            }

            public static LoginToken VerifyTokenString(string token)
            {
                try
                {
                    var protector = GetProtector(Purpose);
                    var base64PlainToken = protector.Unprotect(token);

                    return FromBase64(base64PlainToken);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            private string ToBase64()
            {
                using (var ms = new System.IO.MemoryStream())
                using (var bw = new System.IO.BinaryWriter(ms))
                {
                    bw.Write(TenantID);
                    bw.Write(TenantName);
                    bw.Write(UserID);
                    bw.Write(UserName);
                    bw.Write(CreateTime);
                    bw.Write(Claims.Count);
                    foreach (var claim in Claims)
                    {
                        bw.Write(claim.Key);
                        bw.Write(claim.Value.Count);
                        foreach (var item in claim.Value)
                        {
                            bw.Write(item);
                        }
                    }
                    bw.Flush();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }

            private static LoginToken FromBase64(string str)
            {
                var result = new LoginToken();

                using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(str)))
                using (var br = new System.IO.BinaryReader(ms))
                {
                    result.TenantID = br.ReadInt32();
                    result.TenantName = br.ReadString();
                    result.UserID = br.ReadInt32();
                    result.UserName = br.ReadString();
                    result.CreateTime = br.ReadInt64();
                    result.Claims = new Dictionary<string, List<string>>();
                    var claimsCount = br.ReadInt32();
                    for (var i = 0; i < claimsCount; i++)
                    {
                        var items = new List<string>();
                        result.Claims.Add(br.ReadString(), items);
                        var itemCount = br.ReadInt32();
                        for (var j = 0; j < itemCount; j++)
                        {
                            items.Add(br.ReadString());
                        }
                    }
                    return result;
                }
            }
        }

        private static IDataProtector GetProtector(string purpose)
        {
            return ServiceProvider.GetDataProtector(purpose);
        }

        public static class ListStringHelper
        {
            public static byte[] ToByteArray(List<string> textData)
            {
                using (var ms = new System.IO.MemoryStream())
                using (var bw = new System.IO.BinaryWriter(ms))
                {
                    bw.Write(textData.Count);
                    for (int i = 0; i < textData.Count; i++)
                    {
                        bw.Write(textData[i]);
                    }
                    bw.Flush();
                    return ms.ToArray();
                }
            }

            public static List<string> FromByteArray(byte[] bytes)
            {
                if (bytes.Length == 0)
                    return new List<string>();

                using (var ms = new System.IO.MemoryStream(bytes))
                using (var br = new System.IO.BinaryReader(ms))
                {
                    var count = br.ReadInt32();

                    var result = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        result.Add(br.ReadString());
                    }
                    return result;
                }
            }
        }
    }
}
