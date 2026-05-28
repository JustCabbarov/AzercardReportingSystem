using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<PasswordResetOTP> PasswordResetOtps { get; set; }
        public DbSet<SqlQuery> SqlQueries { get; set; }


        public DbSet<NotificationThreshold> NotificationThresholds { get; set; }
        public DbSet<EnumCategory> EnumCategories { get; set; }
        public DbSet<EnumValue> EnumValues { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Employee>()
            .HasOne(e => e.AppUser)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.AppUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
