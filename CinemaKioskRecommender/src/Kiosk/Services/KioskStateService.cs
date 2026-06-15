using CinemaKioskRecommender.Application.DTOs;

namespace CinemaKioskRecommender.Kiosk.Services;

public class KioskStateService
{
    public event Action? OnChange;

    public KioskSession CurrentSession { get; private set; } = new();

    public void StartAiSession()
    {
        CurrentSession = new KioskSession
        {
            SessionId = Guid.NewGuid()
        };

        NotifyStateChanged();
    }

    public void EndAiSession()
    {
        CurrentSession = new KioskSession();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
