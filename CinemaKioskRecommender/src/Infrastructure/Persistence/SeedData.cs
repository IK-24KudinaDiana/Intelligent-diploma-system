using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Enums;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        var hasMovies = await context.Movies.AnyAsync();
        var hasHalls = await context.Halls.AnyAsync();
        var hasSessions = await context.Sessions.AnyAsync();
        var hasSeats = await context.Seats.AnyAsync();

        if (hasMovies && hasHalls && hasSessions && hasSeats)
        {
            await RefreshSessionDatesIfNeededAsync(context);
            Console.WriteLine("✅ База даних вже містить дані. Seed пропущено.");
            return;
        }

        Console.WriteLine("🌱 Початок заповнення бази даних...");

        if (!hasMovies)
        {
        var movies = new List<Movie>
        {

           
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Диявол носить Прада 2",
                    Description = "Продовження легендарної комедії про світ моди.",
                    DurationMinutes = 130,
                    PosterUrl = "https://multiplex.ua/images/e7/97/e7977d74a70c51c06855250360df9b2c.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=2FIN5utrcbg&time_continue=2&source_ve_path=NzY3NTg&embeds_referring_euri=https%3A%2F%2Fmultiplex.ua%2F",
                    Genres = GenreMaskCodec.Encode([Genre.Comedy, Genre.Drama]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Майкл",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/6c/8d/6c8d360df265e8937be90b8d130262a2.jpeg",
                    TrailerUrl = "youtube.com/watch?v=owMrADtLd0g&source_ve_path=OTY3MTQ&embeds_referring_euri=https%3A%2F%2Fmultiplex.ua%2F",
                    Genres = GenreMaskCodec.Encode([Genre.Drama]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Мортал Комбат ІІ",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/ce/9d/ce9dad7e07e737262ca50306a0026226.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=UXJ_ujJuMjc",
                    Genres = GenreMaskCodec.Encode([Genre.Action, Genre.Fantasy]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "КІЛЛХАУС",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/36/bd/36bd5b7e48974e483750bf9e430a1463.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=GYSi3z7zKD4",
                    Genres = GenreMaskCodec.Encode([Genre.Horror, Genre.Thriller]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Вівці - детективи",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/36/1d/361da4af43a3f0a2bee2d7a3940a6bd6.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=VjNsBq9SkVw",
                    Genres = GenreMaskCodec.Encode([Genre.Animation, Genre.Comedy]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Супер драйвер",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/9f/f5/9ff51d9eef7a64509cca35daa24cfd35.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=BOAEjbuQzZE",
                    Genres = GenreMaskCodec.Encode([Genre.Action, Genre.Comedy]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "ТОП ҐАН: Меверік",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/d4/0a/d40ace3af709b6b10a79a900665fbcb9.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=-oQtcr29vNo",
                    Genres = GenreMaskCodec.Encode([Genre.Action]),
                    ReleaseYear = 2022
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Дуже дорогі батьки",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/37/c1/37c1c2f5549d2b295226e6e3b6ad6e96.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=_oNm1XDtVn0",
                    Genres = GenreMaskCodec.Encode([Genre.Comedy]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Пограбування: План \"ЛОНДОН\"",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/0a/74/0a744981802430b4b5bdb558915ac03f.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=mI3PpcfQEwA",
                    Genres = GenreMaskCodec.Encode([Genre.Action, Genre.Thriller]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Стрибунці",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/be/0a/be0a39737ebbda8fd9b3166cefd054cf.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=HlZ_W2aiNC0",
                    Genres = GenreMaskCodec.Encode([Genre.Animation]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Психоз: Зламана Реальність",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/0a/63/0a63533df538b8ff8e87272427797e1c.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=xdlnOJUYMoA",
                    Genres = GenreMaskCodec.Encode([Genre.Horror, Genre.Thriller]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Бунтівний дракон",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/0e/6a/0e6adad4556ca0ada2c7aaad007cdaa6.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=UmY40qcPnG4",
                    Genres = GenreMaskCodec.Encode([Genre.Animation, Genre.Fantasy]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "КРАЩИЙ СТРІЛЕЦЬ",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/4c/96/4c96658d0df88349046d3c2eae5256f1.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=KcDXn8b-N1Q",
                    Genres = GenreMaskCodec.Encode([Genre.Action]),
                    ReleaseYear = 2026
                },
                new Movie
                {
                    Id = Guid.NewGuid(),
                    Title = "Ралі: від Парижа до пірамід",
                    Description = "",
                    DurationMinutes = 0,
                    PosterUrl = "https://multiplex.ua/images/ec/1f/ec1f8ef8d85e24fe947353c7aadb8225.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=WErwACCOD64",
                    Genres = GenreMaskCodec.Encode([Genre.Adventure, Genre.Animation]),
                    ReleaseYear = 2026
                },
        };

        await context.Movies.AddRangeAsync(movies);
        await context.SaveChangesAsync();
        Console.WriteLine($"   - {movies.Count} фільмів додано");
        }

        if (!hasHalls)
        {
            var halls = new List<Hall>
            {
                new() { Id = Guid.NewGuid(), Name = "Зал 1", Capacity = 50 },
                new() { Id = Guid.NewGuid(), Name = "Зал 2", Capacity = 50 }
            };

            await context.Halls.AddRangeAsync(halls);
            await context.SaveChangesAsync();
            Console.WriteLine($"   - {halls.Count} залів додано");
        }

        var allMovies = await context.Movies.AsNoTracking().ToListAsync();
        var allHalls = await context.Halls.AsNoTracking().OrderBy(h => h.Name).ToListAsync();
        var hall1 = allHalls[0];
        var hall2 = allHalls.Count > 1 ? allHalls[1] : allHalls[0];

        if (!hasSessions)
        {
        var sessions = new List<Session>();
        var today = DateTime.UtcNow.Date;
        var sessionDays = new[] { today, today.AddDays(1) };
        var showtimes = new[]
        {
            new TimeSpan(6, 37, 0),
            new TimeSpan(9, 37, 0),
            new TimeSpan(12, 37, 0)
        };

        foreach (var movie in allMovies)
        {
            foreach (var day in sessionDays)
            {
                for (var index = 0; index < showtimes.Length; index++)
                {
                    sessions.Add(new Session
                    {
                        Id = Guid.NewGuid(),
                        MovieId = movie.Id,
                        HallId = index % 2 == 0 ? hall1.Id : hall2.Id,
                        StartTime = day.Add(showtimes[index]),
                        Price = 150.00m + (index * 10)
                    });
                }
            }
        }

        await context.Sessions.AddRangeAsync(sessions);
        await context.SaveChangesAsync();
        Console.WriteLine($"   - {sessions.Count} сеансів додано");
        }


        if (!hasSeats)
        {
        var allSessions = await context.Sessions.AsNoTracking().ToListAsync();
        var seats = new List<Seat>();
        string[] rows = { "A", "B", "C", "D", "E" };

        foreach (var session in allSessions)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                for (int i = 1; i <= 10; i++)
                {
                    seats.Add(new Seat
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        SeatNumber = $"{rows[r]}{i}",
                        IsOccupied = false
                    });
                }
            }
        }

        const int batchSize = 250;
        for (var i = 0; i < seats.Count; i += batchSize)
        {
            var batch = seats.Skip(i).Take(batchSize).ToList();
            await context.Seats.AddRangeAsync(batch);
            await context.SaveChangesAsync();
        }

        Console.WriteLine($"   - {seats.Count} місць додано");
        }

        await RefreshSessionDatesIfNeededAsync(context);

        Console.WriteLine("✅ Seed завершено.");
    }

    private static async Task RefreshSessionDatesIfNeededAsync(ApplicationDbContext context)
    {
        var sessions = await context.Sessions.ToListAsync();
        if (sessions.Count == 0)
            return;

        var today = DateTime.UtcNow.Date;
        var earliestDate = sessions.Min(s => s.StartTime.Date);
        if (earliestDate >= today)
            return;

        var daysToShift = (today - earliestDate).Days;
        foreach (var session in sessions)
            session.StartTime = session.StartTime.AddDays(daysToShift);

        await context.SaveChangesAsync();
        Console.WriteLine($"   - дати {sessions.Count} сеансів актуалізовано (+{daysToShift} дн.)");
    }
}