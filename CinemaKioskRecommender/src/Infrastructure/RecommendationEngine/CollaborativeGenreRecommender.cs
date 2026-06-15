using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.RecommendationEngine;

public sealed class CollaborativeGenreRecommender
{
    private readonly ApplicationDbContext _context;

    public CollaborativeGenreRecommender(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Movie>> RecommendMightLikeAsync(
        Guid sessionToken,
        IReadOnlyList<Movie> allMovies,
        int topN = 6,
        IEnumerable<Guid>? excludeMovieIds = null)
    {
        var currentSession = await _context.KioskSessions
            .AsNoTracking()
            .Include(s => s.ClientProfile)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        var currentMask = currentSession?.ClientProfile?.GenreMask;
        if (currentSession is null || !GenreMaskCodec.HasAnyGenre(currentMask))
            return new List<Movie>();

        var exclude = excludeMovieIds?.ToHashSet() ?? new HashSet<Guid>();
        var movieScores = new Dictionary<Guid, double>();

        await ScoreFromClientProfilesAsync(currentMask!, exclude, movieScores);

        if (movieScores.Count == 0)
            return FallbackByGenreOverlap(currentMask!, allMovies, topN, exclude);

        var movieLookup = allMovies.ToDictionary(m => m.Id);

        return movieScores
            .Where(kv => movieLookup.ContainsKey(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Take(topN)
            .Select(kv => movieLookup[kv.Key])
            .ToList();
    }

    private async Task ScoreFromClientProfilesAsync(
        string currentMask,
        HashSet<Guid> exclude,
        Dictionary<Guid, double> movieScores)
    {
        var profiles = await _context.ClientProfiles
            .AsNoTracking()
            .Include(p => p.Purchases)
            .Where(p => p.Purchases.Any())
            .ToListAsync();

        foreach (var profile in profiles)
        {
            if (!GenreMaskCodec.HasAnyGenre(profile.GenreMask))
                continue;

            var similarity = GenreMaskCodec.CosineSimilarity(currentMask, profile.GenreMask);

            if (similarity < 0.15)
                continue;

            foreach (var purchase in profile.Purchases)
            {
                if (exclude.Contains(purchase.MovieId))
                    continue;

                AddScore(movieScores, purchase.MovieId, similarity);
            }
        }
    }

    private static void AddScore(Dictionary<Guid, double> scores, Guid movieId, double value)
        => scores[movieId] = scores.GetValueOrDefault(movieId) + value;

    private static List<Movie> FallbackByGenreOverlap(
        string currentMask,
        IReadOnlyList<Movie> allMovies,
        int topN,
        HashSet<Guid> exclude)
    {
        return allMovies
            .Where(m => !exclude.Contains(m.Id))
            .Select(m => new
            {
                Movie = m,
                Score = GenreMaskCodec.CosineSimilarity(currentMask, m.Genres)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => x.Movie)
            .ToList();
    }
}