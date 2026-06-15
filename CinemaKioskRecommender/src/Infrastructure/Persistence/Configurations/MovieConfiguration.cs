using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaKioskRecommender.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Genres)
            .IsRequired()
            .HasMaxLength(GenreMaskCodec.GenreCount)
            .HasDefaultValue(GenreMaskCodec.EmptyMask);
    }
}
