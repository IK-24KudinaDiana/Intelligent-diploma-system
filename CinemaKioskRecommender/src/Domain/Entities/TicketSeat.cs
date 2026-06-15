namespace CinemaKioskRecommender.Domain.Entities;

public class TicketSeat
{
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;
}
