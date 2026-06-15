using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Enums;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Persistence;

public static class GenreSeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        if (await context.Genres.AnyAsync())
            return;

        var genres = Enum.GetValues<Genre>()
            .Select((genre, index) => new GenreItem
            {
                Id = index + 1,
                Name = genre.ToString()
            })
            .ToList();

        await context.Genres.AddRangeAsync(genres);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Додано {genres.Count} жанрів у таблицю Genres.");
    }
}