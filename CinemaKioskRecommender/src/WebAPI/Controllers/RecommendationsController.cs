using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly IMovieDtoMapper _movieDtoMapper;

    public RecommendationsController(
        IRecommendationService recommendationService,
        IMovieDtoMapper movieDtoMapper)
    {
        _recommendationService = recommendationService;
        _movieDtoMapper = movieDtoMapper;
    }

    [HttpGet("session/{sessionToken}/might-like")]
    public async Task<ActionResult<List<MovieDto>>> GetMightLikeForSession(
        Guid sessionToken,
        [FromQuery] int topN = 6,
        [FromQuery] string? exclude = null)
    {
        IEnumerable<Guid>? excludeIds = null;

        if (!string.IsNullOrWhiteSpace(exclude))
        {
            excludeIds = exclude
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();
        }

        var movies = await _recommendationService.RecommendMightLikeForSessionAsync(
            sessionToken,
            topN,
            excludeIds);

        return Ok(await _movieDtoMapper.MapAsync(movies));
    }
}
