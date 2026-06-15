using CinemaKioskRecommender.Domain.Common;

namespace CinemaKioskRecommender.Domain.Entities;

public class Session : BaseEntity
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
