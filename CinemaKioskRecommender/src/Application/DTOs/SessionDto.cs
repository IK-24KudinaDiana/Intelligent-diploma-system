namespace CinemaKioskRecommender.Application.DTOs;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public Guid HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
    public string StartTimeFormatted => StartTime.ToString("dd.MM HH:mm");
}
