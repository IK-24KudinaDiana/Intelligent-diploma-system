using CinemaKioskRecommender.Domain.Common;
using CinemaKioskRecommender.Domain.Helpers;

namespace CinemaKioskRecommender.Domain.Entities;

public class ClientProfile : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime FirstVisitAt { get; set; } = DateTime.UtcNow;
    public DateTime LastVisitAt { get; set; } = DateTime.UtcNow;

    public string GenreMask { get; set; } = GenreMaskCodec.EmptyMask;
    public List<ClientPurchase> Purchases { get; set; } = new();
    public List<KioskSession> Visits { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
}

public class ClientPurchase
{
    public Guid MovieId { get; set; }
    public Guid? TicketId { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}
