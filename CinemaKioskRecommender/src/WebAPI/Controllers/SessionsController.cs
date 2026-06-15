using Microsoft.AspNetCore.Mvc;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using AutoMapper;
using CinemaKioskRecommender.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IKioskSessionService _kioskSessionService;
    private readonly IRepository<Session> _sessionRepo;
    private readonly IMapper _mapper;

    public SessionsController(IRepository<Session> sessionRepo, IMapper mapper, IKioskSessionService kioskSessionService)
    {
        _sessionRepo = sessionRepo;
        _mapper = mapper;
        _kioskSessionService = kioskSessionService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartKioskSessionRequest request)
    {
        var session = await _kioskSessionService.StartSessionAsync(request.KioskId);
        return Ok(new
        {
            sessionToken = session.SessionToken,
            message = "Сесія успішно розпочата"
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetAllSessions()
    {
        var sessions = await _sessionRepo.Query()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Include(s => s.Seats)
            .ToListAsync();
        return Ok(_mapper.Map<List<SessionDto>>(sessions));
    }

    [HttpGet("movie/{movieId:guid}")]
    public async Task<ActionResult<List<SessionDto>>> GetSessionsByMovie(Guid movieId)
    {
        var sessions = await _sessionRepo.Query()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Include(s => s.Seats)
            .Where(s => s.MovieId == movieId)
            .ToListAsync();

        if (!sessions.Any())
            return NotFound(new { message = "Сеанси для цього фільму не знайдено" });

        return Ok(_mapper.Map<List<SessionDto>>(sessions));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDto>> GetSessionById(Guid id)
    {
        var session = await _sessionRepo.Query()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Include(s => s.Seats)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (session == null)
            return NotFound(new { message = "Сеанс не знайдено" });

        return Ok(_mapper.Map<SessionDto>(session));
    }

    [HttpPost("end/{sessionToken}")]
    public async Task<IActionResult> EndSession(Guid sessionToken)
    {
        await _kioskSessionService.EndSessionAsync(sessionToken);
        return Ok(new { message = "Сесію завершено" });
    }

    [HttpPost("{sessionToken:guid}/genres")]
    public async Task<IActionResult> SetGenres(Guid sessionToken, [FromBody] SetSessionGenresRequest request)
    {
        if (request.ReplaceExisting)
            await _kioskSessionService.SetGenrePreferencesAsync(sessionToken, request.Genres);
        else
            await _kioskSessionService.MergeGenrePreferencesAsync(sessionToken, request.Genres);

        return Ok(new { message = "Жанрові вподобання збережено" });
    }
}

public class StartKioskSessionRequest
{
    public string KioskId { get; set; } = string.Empty;
}

public class SetSessionGenresRequest
{
    public List<string> Genres { get; set; } = new();
    public bool ReplaceExisting { get; set; } = true;
}
