using CinemaKioskRecommender.Domain.Common;

namespace CinemaKioskRecommender.Domain.Entities;

public class KioskSession : BaseEntity
{
    public Guid SessionToken { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string KioskId { get; set; } = "Kiosk-01";
    public Guid? ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}
