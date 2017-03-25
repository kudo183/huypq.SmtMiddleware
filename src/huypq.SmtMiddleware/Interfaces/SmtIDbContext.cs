using Microsoft.EntityFrameworkCore;

namespace huypq.SmtMiddleware
{
    public interface SmtIDbContext<T, T1, T2>
        where T : class, SmtITenant
        where T1 : class, SmtIUser
        where T2 : class, SmtIUserClaim
    {
        DbSet<T> SmtTenant { get; set; }
        DbSet<T1> SmtUser { get; set; }
        DbSet<T2> SmtUserClaim { get; set; }
    }
}
