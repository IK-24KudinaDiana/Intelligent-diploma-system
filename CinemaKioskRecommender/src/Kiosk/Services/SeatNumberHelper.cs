using System.Text.RegularExpressions;

namespace CinemaKioskRecommender.Kiosk.Services;

public static partial class SeatNumberHelper
{
    private static readonly Dictionary<char, char> CyrillicRowToLatin = new()
    {
        ['А'] = 'A', ['а'] = 'A',
        ['Б'] = 'B', ['б'] = 'B',
        ['В'] = 'B', ['в'] = 'B',
        ['С'] = 'C', ['с'] = 'C',
        ['Д'] = 'D', ['д'] = 'D',
        ['Е'] = 'E', ['е'] = 'E',
        ['Є'] = 'E', ['є'] = 'E',
    };

    private static readonly Regex SeatInputSplit = SeatInputSplitRegex();
    private static readonly Regex SeatCodePattern = SeatCodePatternRegex();

    public static List<string> ExpandSeatInputs(IEnumerable<string> rawInputs)
    {
        var expanded = new List<string>();
        foreach (var raw in rawInputs)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var candidates = ExtractAllCandidates(raw);
            if (candidates.Count > 0)
                expanded.AddRange(candidates);
            else
                expanded.Add(raw.Trim());
        }

        return expanded;
    }

    public static string Normalize(string seat, IEnumerable<string>? knownSeats = null)
    {
        var candidate = ExtractCandidate(seat);
        if (candidate is null)
            return seat.Trim().ToUpperInvariant();

        if (knownSeats is not null)
        {
            foreach (var existing in knownSeats)
            {
                if (existing.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    return existing;
            }

            foreach (var existing in knownSeats)
            {
                if (existing.EndsWith(candidate, StringComparison.OrdinalIgnoreCase))
                    return existing;
            }
        }

        return candidate;
    }

    private static List<string> ExtractAllCandidates(string raw)
    {
        var found = new List<string>();
        foreach (var part in SeatInputSplit.Split(raw))
        {
            var candidate = ExtractCandidate(part);
            if (candidate is not null && !found.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                found.Add(candidate);
        }

        if (found.Count > 0)
            return found;

        var normalized = NormalizeSeatText(raw).ToUpperInvariant();
        foreach (Match match in SeatCodePattern.Matches(normalized))
        {
            var candidate = $"{match.Groups[1].Value}{int.Parse(match.Groups[2].Value)}";
            if (!found.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                found.Add(candidate);
        }

        return found;
    }

    private static string? ExtractCandidate(string raw)
    {
        var value = NormalizeSeatText(raw);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var compact = Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]", "");
        var directMatch = Regex.Match(compact, @"^([A-E])(\d{1,2})$");
        if (directMatch.Success)
            return $"{directMatch.Groups[1].Value}{int.Parse(directMatch.Groups[2].Value)}";

        var looseMatch = SeatCodePattern.Match(value.ToUpperInvariant());
        if (looseMatch.Success)
            return $"{looseMatch.Groups[1].Value}{int.Parse(looseMatch.Groups[2].Value)}";

        return null;
    }

    private static string NormalizeSeatText(string text)
    {
        var value = text.Trim();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (CyrillicRowToLatin.TryGetValue(chars[i], out var latin))
                chars[i] = latin;
        }

        value = new string(chars);
        var lower = value.ToLowerInvariant();
        foreach (var (word, letter) in RowWords.OrderByDescending(pair => pair.Key.Length))
        {
            if (!lower.StartsWith(word, StringComparison.Ordinal))
                continue;

            var rest = value[word.Length..].TrimStart(' ', '.', ',', '-');
            return $"{letter}{rest}";
        }

        return value;
    }

    private static readonly Dictionary<string, string> RowWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["а"] = "A",
        ["ей"] = "A",
        ["б"] = "B",
        ["бе"] = "B",
        ["бі"] = "B",
        ["сі"] = "C",
        ["с"] = "C",
        ["ц"] = "C",
        ["де"] = "D",
        ["д"] = "D",
        ["е"] = "E",
    };

    [GeneratedRegex(@"([A-E])\s*(\d{1,2})", RegexOptions.IgnoreCase)]
    private static partial Regex SeatCodePatternRegex();

    [GeneratedRegex(@"(?:\s+(?:і|та|and|a|а)\s+)|[,;]", RegexOptions.IgnoreCase)]
    private static partial Regex SeatInputSplitRegex();
}
