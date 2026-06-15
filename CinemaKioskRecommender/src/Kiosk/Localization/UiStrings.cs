namespace CinemaKioskRecommender.Kiosk.Localization;

public static class UiStrings
{
    public const string Back = "← Назад";
    public const string Continue = "Продовжити";
    public const string Selected = "Обрано";
    public const string Details = "Деталі";
    public const string Book = "Забронювати";
    public const string RecommendationsTab = "Рекомендації";
    public const string AllMoviesTab = "Усі кіно";
    public const string YouMightLike = "Вам можуть сподобатись";
    public const string Confirmed = "ПІДТВЕРДЖЕНО";
    public const string VipIncluded = "VIP-досвід включено";

    public static readonly string[] GenreKeys =
    [
        "Action", "Adventure", "Comedy", "Drama", "SciFi", "Horror",
        "Thriller", "Romance", "Animation", "Documentary", "Fantasy"
    ];

    public static readonly string[] RecommendationTabs =
    [
        RecommendationsTab, AllMoviesTab
    ];

    private static readonly Dictionary<string, string> GenreLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Action"] = "Бойовик",
        ["Adventure"] = "Пригоди",
        ["Comedy"] = "Комедія",
        ["Drama"] = "Драма",
        ["SciFi"] = "Наукова фантастика",
        ["Horror"] = "Жахи",
        ["Thriller"] = "Трилер",
        ["Romance"] = "Романтика",
        ["Animation"] = "Анімація",
        ["Documentary"] = "Документальний",
        ["Fantasy"] = "Фентезі"
    };

    public static string Genre(string key)
        => GenreLabels.TryGetValue(key, out var label) ? label : key;
}
