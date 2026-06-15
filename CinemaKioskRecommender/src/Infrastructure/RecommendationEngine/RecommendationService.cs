using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Infrastructure.RecommendationEngine;

public class RecommendationService : IRecommendationService
{
    private readonly IRepository<Movie> _movieRepo;
    private readonly CollaborativeGenreRecommender _collaborativeRecommender;

    public RecommendationService(
        IRepository<Movie> movieRepo,
        CollaborativeGenreRecommender collaborativeRecommender)
    {
        _movieRepo = movieRepo;
        _collaborativeRecommender = collaborativeRecommender;
    }

    public async Task<List<Movie>> RecommendMightLikeForSessionAsync(
        Guid sessionToken,
        int topN = 6,
        IEnumerable<Guid>? excludeMovieIds = null)
    {
        var allMovies = await _movieRepo.GetAllAsync();
        return await _collaborativeRecommender.RecommendMightLikeAsync(
            sessionToken,
            allMovies,
            topN,
            excludeMovieIds);
    }
}