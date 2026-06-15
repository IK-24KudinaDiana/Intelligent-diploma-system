using AutoMapper;
using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IRepository<Ticket> _ticketRepo;
    private readonly IRepository<Seat> _seatRepo;
    private readonly IClientProfileService _clientProfileService;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public TicketsController(
        IRepository<Ticket> ticketRepo,
        IRepository<Seat> seatRepo,
        IClientProfileService clientProfileService,
        ApplicationDbContext context,
        IMapper mapper)
    {
        _ticketRepo = ticketRepo;
        _seatRepo = seatRepo;
        _clientProfileService = clientProfileService;
        _context = context;
        _mapper = mapper;
    }

    [HttpPost("book")]
    public async Task<ActionResult<TicketDto>> BookTicket([FromBody] CreateTicketRequest request)
    {
        var session = await _context.Sessions
            .Include(s => s.Movie)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session is null)
            return NotFound("Session not found");

        var requestedSeatNumbers = request.SeatNumbers.Distinct().ToList();
        var seats = await _seatRepo.FindAsync(s =>
            s.SessionId == request.SessionId &&
            requestedSeatNumbers.Contains(s.SeatNumber));

        if (seats.Count != requestedSeatNumbers.Count)
            return BadRequest("Some seats were not found.");

        if (seats.Any(s => s.IsOccupied))
            return BadRequest("Some seats are already occupied.");

        foreach (var seat in seats)
            seat.IsOccupied = true;

        Guid? kioskSessionInternalId = null;
        if (request.KioskSessionId.HasValue)
        {
            var kioskSession = await _context.KioskSessions
                .FirstOrDefaultAsync(s => s.SessionToken == request.KioskSessionId.Value);
            kioskSessionInternalId = kioskSession?.Id;
        }

        var normalizedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : _clientProfileService.NormalizePhone(request.PhoneNumber);

        var ticket = new Ticket
        {
            SessionId = request.SessionId,
            ClientProfileId = request.ClientProfileId,
            TotalPrice = requestedSeatNumbers.Count * session.Price,
            PurchasedAt = DateTime.UtcNow,
            KioskSessionId = kioskSessionInternalId,
            TicketSeats = seats.Select(seat => new TicketSeat
            {
                SeatId = seat.Id
            }).ToList()
        };

        await _ticketRepo.AddAsync(ticket);
        await _seatRepo.SaveChangesAsync();

        if (request.KioskSessionId.HasValue && !string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var profile = await _clientProfileService.CompleteVisitAsync(
                request.KioskSessionId.Value,
                normalizedPhone,
                session.MovieId,
                ticket.Id);

            ticket.ClientProfileId = profile.Id;
            await _seatRepo.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(normalizedPhone) && !ticket.ClientProfileId.HasValue)
        {
            var profile = await GetOrCreateClientProfileAsync(normalizedPhone);
            ticket.ClientProfileId = profile.Id;
            await _seatRepo.SaveChangesAsync();
        }

        var savedTicket = await _context.Tickets
            .Include(t => t.TicketSeats)
            .ThenInclude(ts => ts.Seat)
            .Include(t => t.Session)
            .ThenInclude(s => s.Movie)
            .Include(t => t.Session)
            .ThenInclude(s => s.Hall)
            .FirstAsync(t => t.Id == ticket.Id);

        return Ok(_mapper.Map<TicketDto>(savedTicket));
    }

    private async Task<ClientProfile> GetOrCreateClientProfileAsync(string normalizedPhone)
    {
        var profile = await _context.ClientProfiles
            .FirstOrDefaultAsync(p => p.PhoneNumber == normalizedPhone);

        if (profile is not null)
            return profile;

        profile = new ClientProfile
        {
            PhoneNumber = normalizedPhone,
            FirstVisitAt = DateTime.UtcNow,
            LastVisitAt = DateTime.UtcNow
        };

        _context.ClientProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Tickets",
            $"{id}.pdf");

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

        return File(bytes, "application/pdf", $"ticket-{id}.pdf");
    }
}
