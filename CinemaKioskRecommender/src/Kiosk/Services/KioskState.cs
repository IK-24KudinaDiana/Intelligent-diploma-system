using CinemaKioskRecommender.Application.DTOs;
using System.Net.Http.Json;

namespace CinemaKioskRecommender.Kiosk.Services;

public sealed class KioskState
{
    private readonly List<string> _selectedSeats = new();
    private readonly List<string> _preferredGenres = new();

    public Guid? SessionToken { get; private set; }
    public string? KioskId { get; private set; }
    public IReadOnlyList<string> PreferredGenres => _preferredGenres;
    public MovieDto? SelectedMovie { get; private set; }
    public IReadOnlyList<string> SelectedSeats => _selectedSeats;
    public TicketDto? LastTicket { get; private set; }
    public string? ConfirmedPhoneNumber { get; private set; }
    public bool IsPurchasePersisted { get; private set; }
    public Guid? CurrentSessionId { get; private set; }
    public string CurrentMovieTitle { get; private set; } = string.Empty;
    public string CurrentHallName { get; private set; } = string.Empty;
    public DateTime? CurrentSessionTime { get; private set; }
    public decimal CurrentPricePerTicket { get; private set; }
    public decimal TotalPrice => SelectedSeats.Count * CurrentPricePerTicket;
    public Guid? CurrentKioskSessionId => SessionToken;
    public Guid? AiSessionId { get; private set; }
    public bool IsSessionActive => SessionToken.HasValue;

    public event Action? OnChange;

    public void SetAiSession(Guid sessionId)
    {
        AiSessionId = sessionId;
        Notify();
    }

    public async Task StartNewSessionAsync(string kioskId, HttpClient httpClient)
    {
        KioskId = kioskId;

        var response = await httpClient.PostAsJsonAsync("api/sessions/start", new { kioskId });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StartSessionResponse>();
        if (result?.SessionToken != null)
        {
            SessionToken = result.SessionToken;
            Notify();
        }
    }

    public async Task EndCurrentSessionAsync(HttpClient httpClient)
    {
        if (!SessionToken.HasValue)
        {
            Reset();
            return;
        }

        try
        {
            await httpClient.PostAsync($"api/sessions/end/{SessionToken.Value}", null);
        }
        catch
        {
        }

        Reset();
    }

    public void SetPreferredGenres(IEnumerable<string> genres)
    {
        _preferredGenres.Clear();
        _preferredGenres.AddRange(genres.Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase));
        Notify();
    }

    public void SelectMovie(MovieDto movie)
    {
        SelectedMovie = movie;
        _selectedSeats.Clear();
        LastTicket = null;
        Notify();
    }

    public void SetSelectedSeats(IEnumerable<string> seats) => SetSeats(seats);

    private void SetSeats(IEnumerable<string> seats)
    {
        _selectedSeats.Clear();
        _selectedSeats.AddRange(seats.Distinct().OrderBy(s => s));
        Notify();
    }

    public void SetCurrentSession(Guid sessionId, string movieTitle, string hallName, DateTime startTime, decimal pricePerTicket)
    {
        CurrentSessionId = sessionId;
        CurrentMovieTitle = movieTitle;
        CurrentHallName = hallName;
        CurrentSessionTime = startTime;
        CurrentPricePerTicket = pricePerTicket;
        Notify();
    }

    public void SetLastTicket(TicketDto ticket)
    {
        LastTicket = ticket;
        Notify();
    }

    public void SetConfirmedPhoneNumber(string phoneNumber)
    {
        ConfirmedPhoneNumber = phoneNumber;
        Notify();
    }

    public void MarkPurchasePersisted()
    {
        IsPurchasePersisted = true;
        Notify();
    }

    public void ClearSelectedSeats()
    {
        _selectedSeats.Clear();
        _preferredGenres.Clear();
        Notify();
    }

    public void Reset()
    {
        SessionToken = null;
        KioskId = null;
        _preferredGenres.Clear();
        SelectedMovie = null;
        _selectedSeats.Clear();
        LastTicket = null;
        ConfirmedPhoneNumber = null;
        IsPurchasePersisted = false;
        AiSessionId = null;
        CurrentSessionId = null;
        CurrentMovieTitle = string.Empty;
        CurrentHallName = string.Empty;
        CurrentSessionTime = null;
        CurrentPricePerTicket = 0;
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}

public class StartSessionResponse
{
    public Guid SessionToken { get; set; }
}
