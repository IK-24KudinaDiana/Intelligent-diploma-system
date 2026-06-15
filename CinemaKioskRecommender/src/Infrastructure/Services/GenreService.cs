using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Services;

public class GenreService : IGenreService
{
    private readonly ApplicationDbContext _context;
    private Dictionary<int, string>? _namesById;

    public GenreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureLoadedAsync()
    {
        if (_namesById is not null)
            return;

        var genres = await _context.Genres.AsNoTracking().OrderBy(g => g.Id).ToListAsync();
        _namesById = genres.ToDictionary(g => g.Id, g => g.Name);
    }

    public string EncodeFromNames(IEnumerable<string> genreNames)
        => GenreMaskCodec.EncodeFromNames(genreNames);

    public IReadOnlyList<string> GetNamesFromMask(string? mask)
    {
        if (_namesById is null)
            throw new InvalidOperationException("GenreService is not loaded. Call EnsureLoadedAsync first.");

        return GenreMaskCodec.DecodeToNames(mask, _namesById);
    }
}
