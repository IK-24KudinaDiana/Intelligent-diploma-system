using CinemaKioskRecommender.Domain.Common;

namespace CinemaKioskRecommender.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid? ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    public decimal TotalPrice { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public Guid? KioskSessionId { get; set; }
    public KioskSession? KioskSession { get; set; }

    public ICollection<TicketSeat> TicketSeats { get; set; } = new List<TicketSeat>();
}
