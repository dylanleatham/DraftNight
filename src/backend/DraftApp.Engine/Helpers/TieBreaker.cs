using DraftApp.Engine.Models;

namespace DraftApp.Engine.Helpers;

/// <summary>
/// Deterministic tie-breaking comparison logic.
/// Standard order: MW desc, seed asc, id asc.
/// </summary>
internal static class TieBreaker
{
    /// <summary>
    /// Gets comparer instance for use with LINQ OrderBy.
    /// </summary>
    public static IComparer<Player> Comparer { get; } = new PlayerComparer();

    /// <summary>
    /// Compares two players using the standard tie-breaking rules.
    /// Returns negative if x ranks higher, positive if y ranks higher, 0 if equal.
    /// </summary>
    public static int Compare(Player x, Player y)
    {
        // 1. Match wins descending (higher MW = better rank)
        var mwCompare = y.MatchWins.CompareTo(x.MatchWins);
        if (mwCompare != 0)
        {
            return mwCompare;
        }

        // 2. Seed ascending (lower seed = better rank)
        var seedCompare = x.Seed.CompareTo(y.Seed);
        if (seedCompare != 0)
        {
            return seedCompare;
        }

        // 3. ID ascending (lexicographic)
        return string.Compare(x.Id, y.Id, StringComparison.Ordinal);
    }

    private sealed class PlayerComparer : IComparer<Player>
    {
        public int Compare(Player? x, Player? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            return TieBreaker.Compare(x, y);
        }
    }
}
