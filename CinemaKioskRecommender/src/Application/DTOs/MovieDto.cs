namespace CinemaKioskRecommender.Application.DTOs;

public class MovieDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string Genres { get; set; } = string.Empty;
    public List<string> GenreNames { get; set; } = new();
    public int ReleaseYear { get; set; }
}
