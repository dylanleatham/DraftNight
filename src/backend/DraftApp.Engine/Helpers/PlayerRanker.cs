using DraftApp.Engine.Models;

namespace DraftApp.Engine.Helpers;

/// <summary>
/// Utility for ranking players deterministically.
/// </summary>
internal static class PlayerRanker
{
    /// <summary>
    /// Returns players ranked by MW desc, seed asc, id asc.
    /// </summary>
    public static IReadOnlyList<Player> Rank(IEnumerable<Player> players) =>
        players.OrderBy(p => p, TieBreaker.Comparer).ToList();

    /// <summary>
    /// Returns active (non-dropped) players ranked by MW desc, seed asc, id asc.
    /// </summary>
    public static IReadOnlyList<Player> RankActive(EventState state) =>
        Rank(state.GetActivePlayers());

    /// <summary>
    /// Returns players in seed order.
    /// </summary>
    public static IReadOnlyList<Player> BySeed(IEnumerable<Player> players) =>
        players.OrderBy(p => p.Seed).ThenBy(p => p.Id).ToList();
}
