using huypq.SmtMiddleware.Entities;
using Microsoft.EntityFrameworkCore;

namespace huypq.SmtMiddleware
{
    public interface IDbContext
    {
        DbSet<SmtDeletedItem> SmtDeletedItem { get; set; }
        DbSet<SmtTable> SmtTable { get; set; }
    }

    public interface IDbContext<T> : IDbContext
        where T : class, ITenant
    {
        DbSet<T> SmtTenant { get; set; }
    }

    public interface IDbContext<T, T1> : IDbContext<T>
        where T : class, ITenant
        where T1 : class, IUser
    {
        DbSet<T1> SmtUser { get; set; }
    }

    public interface IDbContext<T, T1, T2> : IDbContext<T, T1>
        where T : class, ITenant
        where T1 : class, IUser
        where T2 : class, IUserClaim
    {
        DbSet<T2> SmtUserClaim { get; set; }
    }
}
