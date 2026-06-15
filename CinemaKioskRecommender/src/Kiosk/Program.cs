using System.Globalization;
using CinemaKioskRecommender.Kiosk;
using CinemaKioskRecommender.Kiosk.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var ukCulture = new CultureInfo("uk-UA");
CultureInfo.DefaultThreadCurrentCulture = ukCulture;
CultureInfo.DefaultThreadCurrentUICulture = ukCulture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<KioskState>();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5146/")
});

builder.Services.AddScoped<KioskStateService>();
builder.Services.AddScoped<LiveKitInteropService>();
builder.Services.AddScoped<AiCommandListenerService>();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowTransitionDuration = 280;
    config.SnackbarConfiguration.HideTransitionDuration = 220;
    config.SnackbarConfiguration.VisibleStateDuration = 3200;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

await builder.Build().RunAsync();
