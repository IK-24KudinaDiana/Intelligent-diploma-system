using CinemaKioskRecommender.Domain.Common;

namespace CinemaKioskRecommender.Domain.Entities;

public class Seat : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string SeatNumber { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }

    public ICollection<TicketSeat> TicketSeats { get; set; } = new List<TicketSeat>();
}
