using JustAskIndia.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JustAskIndia.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Enables compatibility for older timestamp behavior in Npgsql
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        // Custom DbSets
        public DbSet<UserLogin> AppUserLogins { get; set; }
        public DbSet<Business> Businesses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Required for pgvector extension
            modelBuilder.HasPostgresExtension("vector");

            // Configure pgvector on the Embedding column
            modelBuilder.Entity<Business>()
                .Property(b => b.Embedding)
                .HasColumnType("vector(1536)");

            // Map UserLogin to a custom table name
            modelBuilder.Entity<UserLogin>().ToTable("UserLogins");

            base.OnModelCreating(modelBuilder);
        }
    }
}
