using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Helpers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IRepository<Movie> _repo;
    private readonly IMovieDtoMapper _movieDtoMapper;

    public MoviesController(
        IRepository<Movie> repo,
        IMovieDtoMapper movieDtoMapper)
    {
        _repo = repo;
        _movieDtoMapper = movieDtoMapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<MovieDto>>> GetAll()
        => Ok(await _movieDtoMapper.MapAsync(await _repo.GetAllAsync()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovieDto>> GetById(Guid id)
    {
        var movie = await _repo.GetByIdAsync(id);
        if (movie == null) return NotFound();
        return Ok(await _movieDtoMapper.MapAsync(movie));
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<List<MovieDto>>> GetRecommendations(
        [FromQuery] string? genre = null,
        [FromQuery] string? genres = null)
    {
        var movies = (await _repo.GetAllAsync()).ToList();
        var genreFilters = ParseGenreFilters(genre, genres);

        if (genreFilters.Count > 0)
        {
            movies = movies
                .Where(m => GenreMaskCodec.MatchesAnyGenre(m.Genres, genreFilters))
                .ToList();
        }

        return Ok(await _movieDtoMapper.MapAsync(movies.Take(12)));
    }

    private static HashSet<string> ParseGenreFilters(string? genre, string? genres)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTokens(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                result.Add(token);
        }

        AddTokens(genre);
        AddTokens(genres);
        return result;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<MovieDto>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(new List<MovieDto>());

        var movies = await _repo.GetAllAsync();
        var normalizedQuery = NormalizeTitle(query);

        var result = movies
            .Select(m => new { Movie = m, Score = ScoreTitleMatch(m.Title, normalizedQuery) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => x.Movie)
            .ToList();

        return Ok(await _movieDtoMapper.MapAsync(result));
    }

    private static string NormalizeTitle(string value)
    {
        var chars = value.ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static double ScoreTitleMatch(string title, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return 0;

        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var queryTokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleTokens = normalizedTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryTokens.Length == 0 || titleTokens.Length == 0)
            return 0;

        var overlap = queryTokens.Count(token =>
            titleTokens.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase)
                                 || token.Contains(t, StringComparison.OrdinalIgnoreCase)));

        var tokenScore = (double)overlap / queryTokens.Length;
        var compactQuery = normalizedQuery.Replace(" ", string.Empty);
        var compactTitle = normalizedTitle.Replace(" ", string.Empty);

        if (compactTitle.Contains(compactQuery, StringComparison.OrdinalIgnoreCase))
            tokenScore = Math.Max(tokenScore, 0.95);

        return tokenScore >= 0.4 ? tokenScore : 0;
    }
}
