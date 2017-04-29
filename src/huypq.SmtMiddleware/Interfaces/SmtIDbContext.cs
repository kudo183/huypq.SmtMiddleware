using huypq.SmtMiddleware.Entities;
using Microsoft.EntityFrameworkCore;

namespace huypq.SmtMiddleware
{
    public interface SmtIDbContext
    {
        DbSet<SmtDeletedItem> SmtDeletedItem { get; set; }
        DbSet<SmtTable> SmtTable { get; set; }
    }

    public interface SmtIDbContext<T> : SmtIDbContext
        where T : class, SmtITenant
    {
        DbSet<T> SmtTenant { get; set; }
    }

    public interface SmtIDbContext<T, T1> : SmtIDbContext<T>
        where T : class, SmtITenant
        where T1 : class, SmtIUser
    {
        DbSet<T1> SmtUser { get; set; }
    }

    public interface SmtIDbContext<T, T1, T2> : SmtIDbContext<T, T1>
        where T : class, SmtITenant
        where T1 : class, SmtIUser
        where T2 : class, SmtIUserClaim
    {
        DbSet<T2> SmtUserClaim { get; set; }
    }
}
