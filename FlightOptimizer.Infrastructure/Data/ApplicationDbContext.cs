using FlightOptimizer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightOptimizer.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Airport> Airports { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RestrictedZone> RestrictedZones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Airport>()
                .HasIndex(a => a.IataCode)
                .IsUnique();

            modelBuilder.Entity<Route>()
                .Property(r => r.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Route>()
                .HasOne(r => r.SourceAirport)
                .WithMany()
                .HasForeignKey(r => r.SourceAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Route>()
                .HasOne(r => r.DestAirport)
                .WithMany()
                .HasForeignKey(r => r.DestAirportId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
