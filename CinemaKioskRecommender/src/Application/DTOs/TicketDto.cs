namespace CinemaKioskRecommender.Application.DTOs;

public class TicketDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? ClientProfileId { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime SessionStartTime { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}
