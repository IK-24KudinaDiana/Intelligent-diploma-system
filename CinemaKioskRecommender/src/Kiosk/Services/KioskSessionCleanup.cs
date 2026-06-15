using Microsoft.AspNetCore.Components;

namespace CinemaKioskRecommender.Kiosk.Services;

public static class KioskSessionCleanup
{
    public static async Task EndSessionAndReturnHomeAsync(
        KioskState state,
        KioskStateService stateService,
        AiCommandListenerService aiListener,
        LiveKitInteropService liveKit,
        HttpClient http,
        NavigationManager nav)
    {
        if (state.AiSessionId is not null)
        {
            aiListener.Stop();
            try { await liveKit.DisconnectAsync(); } catch { }
            stateService.EndAiSession();
        }

        state.ClearSelectedSeats();
        await state.EndCurrentSessionAsync(http);
        nav.NavigateTo("/");
    }
}
