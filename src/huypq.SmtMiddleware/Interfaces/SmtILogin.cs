using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace huypq.SmtMiddleware
{
    public interface SmtILogin
    {
        bool IsConfirmed { get; set; }
        bool IsLocked { get; set; }
        string Email { get; set; }
        string PasswordHash { get; set; }
        long TokenValidTime { get; set; }
    }
}
