namespace CinemaKioskRecommender.Application.Interfaces;

public interface IGenreService
{
    Task EnsureLoadedAsync();
    string EncodeFromNames(IEnumerable<string> genreNames);
    IReadOnlyList<string> GetNamesFromMask(string? mask);
}
