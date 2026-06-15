using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Application.Interfaces;

public interface IKioskSessionService
{
    Task<KioskSession> StartSessionAsync(string kioskId);
    Task EndSessionAsync(Guid sessionToken);
    Task MergeGenrePreferencesAsync(Guid sessionToken, IEnumerable<string> genreNames);
    Task SetGenrePreferencesAsync(Guid sessionToken, IEnumerable<string> genreNames);
}
