using CinemaKioskRecommender.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CinemaKioskRecommender.Kiosk.Services;

public sealed class AiCommandListenerService : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly KioskState _state;
    private readonly NavigationManager _nav;
    private readonly ILogger<AiCommandListenerService> _logger;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _running;

    public event Func<AiCommandDto, Task>? CommandReceived;

    public AiCommandListenerService(
        HttpClient http,
        KioskState state,
        NavigationManager nav,
        ILogger<AiCommandListenerService> logger)
    {
        _http = http;
        _state = state;
        _nav = nav;
        _logger = logger;
    }

    public void EnsureStarted()
    {
        if (_running || _state.AiSessionId is null)
            return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(400));
        _running = true;
        _ = ListenAsync(_cts.Token);
        _logger.LogInformation("AI command listener started for {SessionId}", _state.AiSessionId);
    }

    public void Stop()
    {
        if (!_running)
            return;

        _cts?.Cancel();
        _timer?.Dispose();
        _cts?.Dispose();
        _timer = null;
        _cts = null;
        _running = false;
        _logger.LogInformation("AI command listener stopped");
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (_timer is not null && await _timer.WaitForNextTickAsync(token))
        {
            if (_state.AiSessionId is null)
                continue;

            try
            {
                var commands = await _http.GetFromJsonAsync<List<AiCommandDto>>(
                    $"api/ai/{_state.AiSessionId}/commands",
                    token);

                if (commands is not { Count: > 0 })
                    continue;

                foreach (var command in commands)
                {
                    _logger.LogInformation("AI command: {Command}", command.Command);
                    await HandleGlobalCommandAsync(command);

                    if (CommandReceived is not null)
                        await CommandReceived.Invoke(command);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI command polling failed");
            }
        }
    }

    private async Task HandleGlobalCommandAsync(AiCommandDto command)
    {
        switch (command.Command)
        {
            case "select_genres":
            {
                var genres = AiPayloadHelper.GetStringList(command.Payload, "genres");
                if (genres.Count == 0)
                    break;

                var merged = _state.PreferredGenres
                    .Concat(genres)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _state.SetPreferredGenres(merged);

                if (_state.SessionToken.HasValue)
                {
                    try
                    {
                        await _http.PostAsJsonAsync(
                            $"api/sessions/{_state.SessionToken.Value}/genres",
                            new { genres, preferenceLevel = 8, replaceExisting = false });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to persist AI genre preferences");
                    }
                }
                break;
            }

            case "proceed_to_recommendations":
                if (!_nav.Uri.Contains("/recommendations", StringComparison.OrdinalIgnoreCase))
                    _nav.NavigateTo("/recommendations");
                break;

            case "sync_recommendations":
                break;

            case "select_movie":
                await HandleSelectMovieAsync(command);
                break;

            case "select_session":
                await HandleSelectSessionAsync(command);
                break;

            case "proceed_to_payment":
                if (_state.CurrentSessionId.HasValue && _state.SelectedSeats.Any())
                    _nav.NavigateTo($"/payment/{_state.CurrentSessionId}");
                break;

            case "print_ticket":
                break;
        }

        await Task.CompletedTask;
    }

    private async Task HandleSelectMovieAsync(AiCommandDto command)
    {
        if (AiPayloadHelper.TryGetGuid(command.Payload, out var movieId, "movie_id"))
        {
            var movie = await _http.GetFromJsonAsync<MovieDto>($"api/movies/{movieId}");
            if (movie is not null)
                _state.SelectMovie(movie);

            _nav.NavigateTo($"/sessions/{movieId}");
            return;
        }

        var movieName = AiPayloadHelper.GetString(command.Payload, "title", "movie_name", "original_input");
        if (string.IsNullOrWhiteSpace(movieName))
            return;

        var searchResults = await _http.GetFromJsonAsync<List<MovieDto>>(
            $"api/movies/search?query={Uri.EscapeDataString(movieName)}");

        var found = searchResults?.FirstOrDefault();
        if (found is null)
        {
            _logger.LogWarning("Movie not found for AI selection: {Name}", movieName);
            return;
        }

        _state.SelectMovie(found);
        _nav.NavigateTo($"/sessions/{found.Id}");
    }

    private async Task HandleSelectSessionAsync(AiCommandDto command)
    {
        if (!AiPayloadHelper.TryGetGuid(command.Payload, out var sessionId, "session_id"))
        {
            _logger.LogWarning("select_session without resolved session_id");
            return;
        }

        var session = await _http.GetFromJsonAsync<SessionDto>($"api/sessions/{sessionId}");
        if (session is null)
            return;

        _state.SetCurrentSession(
            session.Id,
            session.MovieTitle,
            session.HallName,
            session.StartTime,
            session.Price);

        _nav.NavigateTo($"/seats/{session.Id}");
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await Task.CompletedTask;
    }
}
