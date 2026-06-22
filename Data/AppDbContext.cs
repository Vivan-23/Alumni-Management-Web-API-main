
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

            public DbSet<user> Users { get; set; }
            public DbSet<alumniprofile> AlumniProfiles { get; set; }

            public DbSet<jobposting> JobPostings { get; set; }

            public DbSet<@event> Events { get; set; }
            public DbSet<eventrsvp> EventRSVPs { get; set; }

            public DbSet<donation> Donations { get; set; }
            public DbSet<DonationWebhookLog> DonationWebhookLogs { get; set; }

            public DbSet<notification> Notifications { get; set; }
            public DbSet<auditlog> AuditLogs { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Idempotency key for webhook replay protection
                modelBuilder.Entity<DonationWebhookLog>()
                    .HasIndex(w => w.RazorpayEventId)
                    .IsUnique();
            }
        }
    }
}
