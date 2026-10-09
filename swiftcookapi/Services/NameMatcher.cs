namespace swiftcookapi.Services
{
    // Backend port of the frontend's utils/similarity.ts (Ticket 7) so imports
    // suggest near matches with the same rules as the manual ingredient form.
    public static class NameMatcher
    {
        public static int LevenshteinDistance(string a, string b)
        {
            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) previous[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                }
                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }

        // Closest candidate within maxDistance, excluding exact (case-insensitive)
        // matches. Names under 3 characters are never matched.
        public static T? FindClosest<T>(string name, IEnumerable<T> candidates, Func<T, string> nameOf, int maxDistance = 2)
            where T : class
        {
            var lower = name.Trim().ToLowerInvariant();
            if (lower.Length < 3) return null;

            T? best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in candidates)
            {
                var candidateLower = nameOf(candidate).ToLowerInvariant();
                if (candidateLower == lower || candidateLower.Length < 3) continue;

                var distance = LevenshteinDistance(lower, candidateLower);
                if (distance == 0 || distance > maxDistance) continue;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
