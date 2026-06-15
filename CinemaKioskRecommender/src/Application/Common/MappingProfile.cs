using AutoMapper;
using CinemaKioskRecommender.Application.DTOs;
using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Movie, MovieDto>()
            .ForMember(dest => dest.TrailerUrl, opt => opt.MapFrom(src => src.TrailerUrl));

        CreateMap<Session, SessionDto>()
            .ForMember(dest => dest.MovieTitle, opt => opt.MapFrom(src => src.Movie.Title))
            .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall.Name));

        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.SeatNumbers, opt => opt.MapFrom(src =>
                src.TicketSeats.Select(ts => ts.Seat.SeatNumber).OrderBy(seatNumber => seatNumber).ToList()))
            .ForMember(dest => dest.MovieTitle, opt =>
                opt.MapFrom(src => src.Session.Movie.Title))
            .ForMember(dest => dest.SessionStartTime, opt =>
                opt.MapFrom(src => src.Session.StartTime))
            .ForMember(dest => dest.HallName, opt =>
                opt.MapFrom(src => src.Session.Hall.Name))
            .ForMember(dest => dest.PosterUrl, opt =>
                opt.MapFrom(src => src.Session.Movie.PosterUrl));

        CreateMap<Seat, SeatDto>();
    }
}
