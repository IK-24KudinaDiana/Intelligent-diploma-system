using MudBlazor;

namespace CinemaKioskRecommender.Kiosk.Services;

public static class KioskToast
{
    public static void Show(ISnackbar snackbar, string message, Severity severity = Severity.Normal)
    {
        snackbar.Add(message, severity, config =>
        {
            config.SnackbarVariant = Variant.Filled;
            config.VisibleStateDuration = 3200;
            config.ShowCloseIcon = true;
            config.HideTransitionDuration = 220;
            config.ShowTransitionDuration = 280;
            config.SnackbarTypeClass = $"kiosk-toast kiosk-toast-{severity.ToString().ToLowerInvariant()}";
        });
    }
}
