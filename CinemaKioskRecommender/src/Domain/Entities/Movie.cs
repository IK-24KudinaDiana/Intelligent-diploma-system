using CinemaKioskRecommender.Domain.Common;
using CinemaKioskRecommender.Domain.Helpers;

namespace CinemaKioskRecommender.Domain.Entities;

public class Movie : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string Genres { get; set; } = GenreMaskCodec.EmptyMask;
    public int ReleaseYear { get; set; }
}
