using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace CinemaKioskRecommender.Kiosk.Services;

public class LiveKitInteropService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private DotNetObjectReference<LiveKitInteropService>? _objRef;

    public LiveKitInteropService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public async Task ConnectAsync(Guid sessionId)
    {
        var tokenResponse = await _httpClient.GetFromJsonAsync<LiveKitTokenResponse>(
            $"api/livekit/token/{sessionId}")
            ?? throw new InvalidOperationException("Unable to get LiveKit token");

        _objRef = DotNetObjectReference.Create(this);

        await _jsRuntime.InvokeVoidAsync(
            "liveKitInterop.connect",
            tokenResponse.Url,
            tokenResponse.Token,
            _objRef);
    }

    public async Task DisconnectAsync()
    {
        await _jsRuntime.InvokeVoidAsync("liveKitInterop.disconnect");
    }

    [JSInvokable]
    public Task OnConnected() => Task.CompletedTask;

    [JSInvokable]
    public Task OnDisconnected() => Task.CompletedTask;

    [JSInvokable]
    public Task OnTrackSubscribed(string kind, string participant) => Task.CompletedTask;

    [JSInvokable]
    public Task OnConnectionError(string errorMessage) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        try { await DisconnectAsync(); } catch { }
        _objRef?.Dispose();
    }
}

public class LiveKitTokenResponse
{
    public string Url { get; set; } = default!;
    public string Token { get; set; } = default!;
}
