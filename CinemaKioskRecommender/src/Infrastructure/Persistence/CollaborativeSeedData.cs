using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Enums;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Persistence;

public static class CollaborativeSeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        if (await context.ClientProfiles.AnyAsync())
        {
            Console.WriteLine("✅ Профілі клієнтів (телефон) уже існують. Seed пропущено.");
            return;
        }

        var movies = await context.Movies.AsNoTracking().ToListAsync();
        if (movies.Count == 0)
        {
            Console.WriteLine("⚠️ Немає фільмів для collaborative seed.");
            return;
        }

        Console.WriteLine("🌱 Заповнення тестових профілів клієнтів за телефоном...");

        var byTitle = movies.ToDictionary(m => m.Title, m => m, StringComparer.OrdinalIgnoreCase);
        Movie M(string title) => byTitle[title];

        var profiles = new List<ClientProfile>
        {
            CreateProfile("+380501234567",
                [Genre.Comedy, Genre.Romance],
                [M("Диявол носить Прада 2"), M("Дуже дорогі батьки"), M("Вівці - детективи")]),

            CreateProfile("+380672345678",
                [Genre.Action, Genre.Thriller, Genre.Fantasy],
                [M("ТОП ҐАН: Меверік"), M("Мортал Комбат ІІ"), M("КРАЩИЙ СТРІЛЕЦЬ")]),

            CreateProfile("+380933456789",
                [Genre.Horror, Genre.Thriller],
                [M("КІЛЛХАУС"), M("Психоз: Зламана Реальність")]),

            CreateProfile("+380504567890",
                [Genre.Animation, Genre.Fantasy, Genre.Adventure],
                [M("Бунтівний дракон"), M("Стрибунці"), M("Ралі: від Парижа до пірамід")]),

            CreateProfile("+380675678901",
                [Genre.Drama, Genre.Romance],
                [M("Майкл"), M("Диявол носить Прада 2")]),

            CreateProfile("+380966789012",
                [Genre.Action, Genre.Comedy],
                [M("Супер драйвер"), M("ТОП ҐАН: Меверік")])
        };

        await context.ClientProfiles.AddRangeAsync(profiles);

        var historicalVisits = new List<KioskSession>
        {
            CreateCompletedVisit("Kiosk-01", profiles[0].Id),
            CreateCompletedVisit("Kiosk-02", profiles[1].Id)
        };

        await context.KioskSessions.AddRangeAsync(historicalVisits);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Додано {profiles.Count} профілів клієнтів і {historicalVisits.Count} завершених візитів.");
    }

    private static ClientProfile CreateProfile(
        string phone,
        Genre[] genres,
        Movie[] purchasedMovies)
    {
        var profile = new ClientProfile
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phone,
            GenreMask = GenreMaskCodec.Encode(genres),
            FirstVisitAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 180)),
            LastVisitAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
        };

        foreach (var movie in purchasedMovies)
        {
            profile.Purchases.Add(new ClientPurchase
            {
                MovieId = movie.Id,
                PurchasedAt = profile.LastVisitAt.AddMinutes(-Random.Shared.Next(10, 60))
            });
        }

        return profile;
    }

    private static KioskSession CreateCompletedVisit(string kioskId, Guid clientProfileId)
    {
        var started = DateTime.UtcNow.AddDays(-Random.Shared.Next(5, 60));

        return new KioskSession
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            ClientProfileId = clientProfileId,
            StartedAt = started,
            EndedAt = started.AddMinutes(Random.Shared.Next(8, 25))
        };
    }
}
