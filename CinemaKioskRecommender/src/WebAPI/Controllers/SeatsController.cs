using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/sessions/{sessionId}/seats")]
public class SeatsController : ControllerBase
{
    private readonly IRepository<Seat> _seatRepository;

    public SeatsController(IRepository<Seat> seatRepository)
    {
        _seatRepository = seatRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<SeatDto>>> GetSeats(Guid sessionId)
    {
        var seats = await _seatRepository.FindAsync(s => s.SessionId == sessionId);

        var dtos = seats.Select(s => new SeatDto
        {
            SeatNumber = s.SeatNumber,
            IsOccupied = s.IsOccupied
        }).ToList();

        return Ok(dtos);
    }
}
