using System.Text;

namespace Domain.Models
{
    public static class SearchNormalizer
    {
        public static string NormalizeTurkish(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text.Trim().ToLowerInvariant())
            {
                sb.Append(ch switch
                {
                    'ğ' => 'g',
                    'ü' => 'u',
                    'ş' => 's',
                    'ı' => 'i',
                    'i' => 'i',
                    'ö' => 'o',
                    'ç' => 'c',
                    _ => ch
                });
            }

            return sb.ToString();
        }

        public static IReadOnlyList<string> Tokenize(string? query)
        {
            var normalized = NormalizeTurkish(query);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Array.Empty<string>();
            }

            return normalized
                .Split(new[] { ' ', ',', ';', '.', '-', '/', '+' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public static bool MatchesAllTokens(string? haystack, IReadOnlyList<string> tokens)
        {
            if (tokens.Count == 0)
            {
                return true;
            }

            var normHaystack = NormalizeTurkish(haystack);
            if (string.IsNullOrWhiteSpace(normHaystack))
            {
                return false;
            }

            foreach (var token in tokens)
            {
                // Basit Turkce ek toleransi: or. "peyniri" -> "peynir"
                var stem = token.Length >= 5 && (token.EndsWith("i") || token.EndsWith("u") || token.EndsWith("a") || token.EndsWith("e"))
                    ? token[..^1]
                    : token;

                if (!normHaystack.Contains(token, StringComparison.Ordinal) &&
                    !normHaystack.Contains(stem, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
