using CinemaKioskRecommender.Domain.Entities;

namespace CinemaKioskRecommender.Application.Interfaces;

public interface IClientProfileService
{
    string NormalizePhone(string phone);

    Task<ClientProfile> CompleteVisitAsync(
        Guid kioskSessionToken,
        string phoneNumber,
        Guid movieId,
        Guid ticketId);
}
