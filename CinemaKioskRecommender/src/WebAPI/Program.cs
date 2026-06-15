using CinemaKioskRecommender.Application.Common;
using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Infrastructure.Persistence;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using CinemaKioskRecommender.Infrastructure.Persistence.Repositories;
using CinemaKioskRecommender.Infrastructure.RecommendationEngine;
using CinemaKioskRecommender.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cinema Kiosk API",
        Version = "v1",
        Description = "API для інтелектуального кіоску рекомендацій фільмів"
    });
});

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);
var databasePath = Path.Combine(dataDirectory, "cinema_kiosk.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath};Cache=Shared;Pooling=True"));

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<CollaborativeGenreRecommender>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IMovieDtoMapper, MovieDtoMapper>();
builder.Services.AddScoped<IKioskSessionService, KioskSessionService>();
builder.Services.AddScoped<IClientProfileService, ClientProfileService>();
builder.Services.AddHttpClient();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddCors(options =>
{
    options.AddPolicy("KioskPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("KioskPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema Kiosk API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await GenreSeedData.InitializeAsync(context);
    await SeedData.InitializeAsync(context);
    await CollaborativeSeedData.InitializeAsync(context);
}

app.Run();
