using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Application.Interfaces;

public interface IMovieDtoMapper
{
    Task<MovieDto> MapAsync(Movie movie);
    Task<List<MovieDto>> MapAsync(IEnumerable<Movie> movies);
}
