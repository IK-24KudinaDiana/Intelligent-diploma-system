using CinemaKioskRecommender.Domain.Enums;

namespace CinemaKioskRecommender.Domain.Helpers;

public static class GenreMaskCodec
{
    public static readonly int GenreCount = Enum.GetValues<Genre>().Length;
    public static readonly string EmptyMask = new('0', GenreCount);

    public static string Encode(IEnumerable<Genre> genres)
    {
        var chars = EmptyMask.ToCharArray();
        foreach (var genre in genres)
        {
            var index = (int)genre;
            if (index >= 0 && index < chars.Length)
                chars[index] = '1';
        }
        return new string(chars);
    }

    public static string EncodeFromNames(IEnumerable<string> genreNames)
    {
        var genres = genreNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => Enum.TryParse<Genre>(n, true, out var g) ? g : (Genre?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value);

        return Encode(genres);
    }

    public static string Merge(string existing, string incoming)
    {
        var left = Normalize(existing);
        var right = Normalize(incoming);
        var chars = new char[GenreCount];

        for (var i = 0; i < GenreCount; i++)
            chars[i] = left[i] == '1' || right[i] == '1' ? '1' : '0';

        return new string(chars);
    }

    public static IReadOnlyList<Genre> DecodeToGenres(string? mask)
    {
        var normalized = Normalize(mask);
        var allGenres = Enum.GetValues<Genre>();
        var result = new List<Genre>();

        for (var i = 0; i < normalized.Length && i < allGenres.Length; i++)
        {
            if (normalized[i] == '1')
                result.Add(allGenres[i]);
        }

        return result;
    }

    public static IReadOnlyList<int> DecodeToIds(string? mask)
    {
        var normalized = Normalize(mask);
        var result = new List<int>();

        for (var i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] == '1')
                result.Add(i + 1);
        }

        return result;
    }

    public static IReadOnlyList<string> DecodeToNames(string? mask, IReadOnlyDictionary<int, string> namesById)
    {
        return DecodeToIds(mask)
            .Where(namesById.ContainsKey)
            .Select(id => namesById[id])
            .ToList();
    }

    public static double[] ToVector(string? mask)
    {
        var normalized = Normalize(mask);
        return normalized.Select(c => c == '1' ? 1.0 : 0.0).ToArray();
    }

    public static double CosineSimilarity(string? left, string? right)
    {
        var lv = ToVector(left);
        var rv = ToVector(right);

        double dot = 0, magLeft = 0, magRight = 0;

        for (var i = 0; i < GenreCount; i++)
        {
            dot += lv[i] * rv[i];
            magLeft += lv[i] * lv[i];
            magRight += rv[i] * rv[i];
        }

        if (magLeft <= 1e-10 || magRight <= 1e-10)
            return 0;

        return dot / (Math.Sqrt(magLeft) * Math.Sqrt(magRight));
    }

    public static bool HasAnyGenre(string? mask)
        => Normalize(mask).Contains('1');

    public static bool MatchesAnyGenre(string? movieMask, IEnumerable<string> genreNames)
    {
        var filterMask = EncodeFromNames(genreNames);
        var movie = Normalize(movieMask);
        var filter = Normalize(filterMask);

        for (var i = 0; i < GenreCount; i++)
        {
            if (filter[i] == '1' && movie[i] == '1')
                return true;
        }

        return false;
    }

    private static string Normalize(string? mask)
    {
        if (string.IsNullOrEmpty(mask))
            return EmptyMask;

        if (mask.Length >= GenreCount)
            return mask[..GenreCount];

        return mask.PadRight(GenreCount, '0');
    }
}
