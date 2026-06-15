using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Application.Interfaces;

public interface IRecommendationService
{
    Task<List<Movie>> RecommendMightLikeForSessionAsync(
        Guid sessionToken,
        int topN = 6,
        IEnumerable<Guid>? excludeMovieIds = null);
}
