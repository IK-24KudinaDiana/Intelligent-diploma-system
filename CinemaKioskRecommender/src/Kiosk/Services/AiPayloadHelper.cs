using System.Text.Json;

namespace CinemaKioskRecommender.Kiosk.Services;

public static class AiPayloadHelper
{
    public static string? GetString(Dictionary<string, object> payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!payload.TryGetValue(key, out var value) || value is null)
                continue;

            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => element.ToString(),
                };
            }

            return value.ToString();
        }

        return null;
    }

    public static bool TryGetGuid(Dictionary<string, object> payload, out Guid guid, params string[] keys)
    {
        guid = default;
        var text = GetString(payload, keys);
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out guid);
    }

    public static List<string> GetStringList(Dictionary<string, object> payload, string key)
    {
        if (!payload.TryGetValue(key, out var value) || value is null)
            return new List<string>();

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item!)
                    .ToList();
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var raw = element.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return new List<string>();

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
                }
                catch
                {
                    return SeatNumberHelper.ExpandSeatInputs(new[] { raw });
                }
            }
        }

        return new List<string>();
    }
}
