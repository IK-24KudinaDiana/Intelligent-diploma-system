using CinemaKioskRecommender.Application.Interfaces;
using CinemaKioskRecommender.Domain.Entities;
using CinemaKioskRecommender.Domain.Helpers;
using CinemaKioskRecommender.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CinemaKioskRecommender.Infrastructure.Services;

public class ClientProfileService : IClientProfileService
{
    private readonly ApplicationDbContext _context;

    public ClientProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("380") && digits.Length >= 12)
            return "+" + digits[..12];

        if (digits.Length == 10)
            return "+38" + digits;

        return digits.Length > 0 ? "+" + digits : string.Empty;
    }

    public async Task<ClientProfile> CompleteVisitAsync(
        Guid kioskSessionToken,
        string phoneNumber,
        Guid movieId,
        Guid ticketId)
    {
        var normalizedPhone = NormalizePhone(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            throw new ArgumentException("Некоректний номер телефону.", nameof(phoneNumber));

        var session = await _context.KioskSessions
            .Include(s => s.ClientProfile)
            .FirstOrDefaultAsync(s => s.SessionToken == kioskSessionToken)
            ?? throw new InvalidOperationException("Кіоск-сесію не знайдено.");

        var sessionProfile = session.ClientProfile;
        var sessionGenreMask = sessionProfile?.GenreMask ?? GenreMaskCodec.EmptyMask;

        var profile = await _context.ClientProfiles
            .Include(p => p.Purchases)
            .FirstOrDefaultAsync(p => p.PhoneNumber == normalizedPhone);

        if (profile is null)
        {
            if (sessionProfile is not null && KioskSessionService.IsAnonymousPhone(sessionProfile.PhoneNumber))
            {
                profile = sessionProfile;
                profile.PhoneNumber = normalizedPhone;
            }
            else
            {
                profile = new ClientProfile
                {
                    PhoneNumber = normalizedPhone,
                    FirstVisitAt = DateTime.UtcNow
                };
                _context.ClientProfiles.Add(profile);
            }
        }

        profile.LastVisitAt = DateTime.UtcNow;
        profile.GenreMask = GenreMaskCodec.Merge(profile.GenreMask, sessionGenreMask);

        profile.Purchases.Add(new ClientPurchase
        {
            MovieId = movieId,
            TicketId = ticketId,
            PurchasedAt = DateTime.UtcNow
        });

        session.ClientProfileId = profile.Id;
        session.ClientProfile = profile;
        session.EndedAt ??= DateTime.UtcNow;

        if (sessionProfile is not null
            && sessionProfile.Id != profile.Id
            && KioskSessionService.IsAnonymousPhone(sessionProfile.PhoneNumber))
        {
            _context.ClientProfiles.Remove(sessionProfile);
        }

        await _context.SaveChangesAsync();
        return profile;
    }
}
