using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddlewareTest
{
    public partial class TestContext : DbContext, SmtIDbContext<SmtTenant,SmtUser,SmtUserClaim>
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SmtTenant>(entity =>
            {
                entity.HasKey(e => e.ID)
                    .HasName("PK_SmtTenant");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(128);
                entity.Property(e => e.TenantName)
                    .IsRequired()
                    .HasMaxLength(256);

            });
            modelBuilder.Entity<SmtUser>(entity =>
            {
                entity.HasKey(e => e.ID)
                    .HasName("PK_SmtUser");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(128);
                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.TenantIDNavigation)
                    .WithMany(p => p.SmtUserTenantIDNavigation)
                    .HasForeignKey(d => d.TenantID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SmtUser_SmtTenant");

            });
            modelBuilder.Entity<SmtUserClaim>(entity =>
            {
                entity.HasKey(e => e.ID)
                    .HasName("PK_SmtUserClaim");

                entity.Property(e => e.Claim)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.TenantIDNavigation)
                    .WithMany(p => p.SmtUserClaimTenantIDNavigation)
                    .HasForeignKey(d => d.TenantID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SmtUserClaim_SmtTenant");

                entity.HasOne(d => d.UserIDNavigation)
                    .WithMany(p => p.SmtUserClaimUserIDNavigation)
                    .HasForeignKey(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SmtUserClaim_SmtUserClaim");

            });
        }
        public DbSet<SmtTenant> SmtTenant { get; set; }
        public DbSet<SmtUser> SmtUser { get; set; }
        public DbSet<SmtUserClaim> SmtUserClaim { get; set; }
    }
}
