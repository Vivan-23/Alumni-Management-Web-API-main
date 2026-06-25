
using AlumniManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace AlumniManagementApi.Data

{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    namespace AlumniManagementApi.Data
    {
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
            {
            }

            public DbSet<User> Users { get; set; }
            public DbSet<Role> Roles { get; set; }
            public DbSet<AlumniProfile> AlumniProfiles { get; set; }

            public DbSet<JobPosting> JobPostings { get; set; }

            public DbSet<@Event> Events { get; set; }
            public DbSet<EventRSVP> EventRSVPs { get; set; }

            public DbSet<Donation> Donations { get; set; }
            public DbSet<DonationWebhookLog> DonationWebhookLogs { get; set; }

            public DbSet<Notification> Notifications { get; set; }
            public DbSet<AuditLog> AuditLogs { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Composite primary key for EventRSVP join table
                modelBuilder.Entity<EventRSVP>()
                    .HasKey(r => new { r.EventId, r.UserId });

                // Idempotency key for webhook replay protection
                modelBuilder.Entity<DonationWebhookLog>()
                    .HasIndex(w => w.RazorpayEventId)
                    .IsUnique();

                // One-to-One: User and AlumniProfile
                modelBuilder.Entity<AlumniProfile>()
                    .HasOne(a => a.User)
                    .WithOne() // Or .WithOne(u => u.AlumniProfile) if added to User class
                    .HasForeignKey<AlumniProfile>(a => a.UserId);

                // Seed Roles
                modelBuilder.Entity<Role>().HasData(
                    new Role { Id = 1, RoleName = "Admin" },
                    new Role { Id = 2, RoleName = "Alumni" },
                    new Role { Id = 3, RoleName = "Student" }
                );
            }

        }
    }
}
