using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware
{
    public class SmtTokenModel
    {
        /// <summary>
        /// Name of the tenant or user
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Id of the logged in user if login is user
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// TenantId of the tenant if login is tenant
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Token creation time
        /// </summary>
        public long CreateTime { get; set; }

        /// <summary>
        /// key: controller, value: list of action
        /// </summary>
        public Dictionary<string, List<string>> Claims { get; set; }

        public SmtTokenModel()
        {
            CreateTime = DateTime.UtcNow.Ticks;
            Claims = new Dictionary<string, List<string>>();
        }

        public string ToBase64()
        {
            using (var ms = new System.IO.MemoryStream())
            using (var bw = new System.IO.BinaryWriter(ms))
            {
                bw.Write(UserId);
                bw.Write(TenantId);
                bw.Write(Name);
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

        public static SmtTokenModel FromBase64(string str)
        {
            var result = new SmtTokenModel();

            using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(str)))
            using (var br = new System.IO.BinaryReader(ms))
            {
                result.UserId = br.ReadInt32();
                result.TenantId = br.ReadInt32();
                result.Name = br.ReadString();
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
}
