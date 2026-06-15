namespace CinemaKioskRecommender.Application.DTOs;

public class SeatDto
{
    public string SeatNumber { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
}

public class CreateTicketRequest
{
    public Guid SessionId { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
    public Guid? KioskSessionId { get; set; }
    public Guid? ClientProfileId { get; set; }
    public string? PhoneNumber { get; set; }
}
