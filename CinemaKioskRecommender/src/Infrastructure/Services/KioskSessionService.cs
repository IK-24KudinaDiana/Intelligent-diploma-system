using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Services;

public class KioskSessionService : IKioskSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IGenreService _genreService;

    public KioskSessionService(ApplicationDbContext context, IGenreService genreService)
    {
        _context = context;
        _genreService = genreService;
    }

    public async Task<KioskSession> StartSessionAsync(string kioskId)
    {
        var session = new KioskSession
        {
            KioskId = kioskId
        };

        _context.KioskSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task EndSessionAsync(Guid sessionToken)
    {
        var session = await _context.KioskSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session != null)
        {
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetGenrePreferencesAsync(Guid sessionToken, IEnumerable<string> genreNames)
    {
        var session = await _context.KioskSessions
            .Include(s => s.ClientProfile)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session is null)
            return;

        var profile = await EnsureSessionProfileAsync(session);
        profile.GenreMask = _genreService.EncodeFromNames(genreNames);
        await _context.SaveChangesAsync();
    }

    public async Task MergeGenrePreferencesAsync(Guid sessionToken, IEnumerable<string> genreNames)
    {
        var session = await _context.KioskSessions
            .Include(s => s.ClientProfile)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session is null)
            return;

        var profile = await EnsureSessionProfileAsync(session);
        var incoming = _genreService.EncodeFromNames(genreNames);
        profile.GenreMask = GenreMaskCodec.Merge(profile.GenreMask, incoming);
        await _context.SaveChangesAsync();
    }

    private async Task<ClientProfile> EnsureSessionProfileAsync(KioskSession session)
    {
        if (session.ClientProfile is not null)
            return session.ClientProfile;

        if (session.ClientProfileId.HasValue)
        {
            var existing = await _context.ClientProfiles
                .FirstOrDefaultAsync(p => p.Id == session.ClientProfileId.Value);

            if (existing is not null)
            {
                session.ClientProfile = existing;
                return existing;
            }
        }

        var profile = new ClientProfile
        {
            PhoneNumber = BuildAnonymousPhone(session.Id),
            FirstVisitAt = DateTime.UtcNow,
            LastVisitAt = DateTime.UtcNow
        };

        _context.ClientProfiles.Add(profile);
        session.ClientProfileId = profile.Id;
        session.ClientProfile = profile;
        await _context.SaveChangesAsync();

        return profile;
    }

    internal static string BuildAnonymousPhone(Guid sessionId)
        => $"+t{sessionId:N}"[..20];

    internal static bool IsAnonymousPhone(string phone)
        => phone.StartsWith("+t", StringComparison.Ordinal);
}
