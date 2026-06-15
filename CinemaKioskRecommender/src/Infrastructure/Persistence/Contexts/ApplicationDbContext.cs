using CinemaKioskRecommender.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<GenreItem> Genres => Set<GenreItem>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketSeat> TicketSeats => Set<TicketSeat>();
    public DbSet<KioskSession> KioskSessions => Set<KioskSession>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<ClientProfile>(entity =>
        {
            entity.HasIndex(p => p.PhoneNumber).IsUnique();
            entity.Property(p => p.PhoneNumber).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<ClientProfile>()
            .OwnsMany(p => p.Purchases, purchase =>
            {
                purchase.WithOwner().HasForeignKey("ClientProfileId");
                purchase.Property<int>("Id");
                purchase.HasKey("Id");
            });

        modelBuilder.Entity<KioskSession>()
            .HasOne(s => s.ClientProfile)
            .WithMany(p => p.Visits)
            .HasForeignKey(s => s.ClientProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.Hall)
            .WithMany(h => h.Sessions)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Session)
            .WithMany(s => s.Seats)
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Session)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.ClientProfile)
            .WithMany(p => p.Tickets)
            .HasForeignKey(t => t.ClientProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ticket>()
            .Property(t => t.TotalPrice)
            .HasColumnType("TEXT")
            .IsRequired();

        modelBuilder.Entity<TicketSeat>()
            .HasKey(ts => new { ts.TicketId, ts.SeatId });

        modelBuilder.Entity<TicketSeat>()
            .HasOne(ts => ts.Ticket)
            .WithMany(t => t.TicketSeats)
            .HasForeignKey(ts => ts.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketSeat>()
            .HasOne(ts => ts.Seat)
            .WithMany(s => s.TicketSeats)
            .HasForeignKey(ts => ts.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
