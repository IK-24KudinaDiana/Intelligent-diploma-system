using CinemaKioskRecommender.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaKioskRecommender.Infrastructure.Persistence.Configurations;

public class GenreItemConfiguration : IEntityTypeConfiguration<GenreItem>
{
    public void Configure(EntityTypeBuilder<GenreItem> builder)
    {
        builder.ToTable("Genres");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(50);
    }
}
