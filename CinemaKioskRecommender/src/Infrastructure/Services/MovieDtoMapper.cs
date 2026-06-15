using AutoMapper;
using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Infrastructure.Services;

public class MovieDtoMapper : IMovieDtoMapper
{
    private readonly IMapper _mapper;
    private readonly IGenreService _genreService;

    public MovieDtoMapper(IMapper mapper, IGenreService genreService)
    {
        _mapper = mapper;
        _genreService = genreService;
    }

    public async Task<MovieDto> MapAsync(Movie movie)
    {
        await _genreService.EnsureLoadedAsync();
        var dto = _mapper.Map<MovieDto>(movie);
        dto.GenreNames = _genreService.GetNamesFromMask(movie.Genres).ToList();
        return dto;
    }

    public async Task<List<MovieDto>> MapAsync(IEnumerable<Movie> movies)
    {
        await _genreService.EnsureLoadedAsync();
        var list = new List<MovieDto>();

        foreach (var movie in movies)
        {
            var dto = _mapper.Map<MovieDto>(movie);
            dto.GenreNames = _genreService.GetNamesFromMask(movie.Genres).ToList();
            list.Add(dto);
        }

        return list;
    }
}
