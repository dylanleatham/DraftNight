using System.Collections.Immutable;
using DraftApp.Engine.Helpers;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Generates round-robin pairings using the circle method.
/// </summary>
internal static class RoundRobinScheduler
{
    /// <summary>
    /// Ghost BYE identifier for odd player counts.
    /// </summary>
    private const string GhostByeId = "__BYE__";

    /// <summary>
    /// Generates all round-robin pairings for the event.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <returns>Dictionary mapping round number to list of matches.</returns>
    public static ImmutableDictionary<int, ImmutableList<Match>> GenerateAllRounds(EventState state)
    {
        var players = PlayerRanker.BySeed(state.Players.Values).ToList();
        var n = players.Count;

        // Add ghost BYE for odd player count
        var ids = players.Select(p => p.Id).ToList();
        if (n % 2 != 0)
        {
            ids.Add(GhostByeId);
        }

        var nEven = ids.Count;
        var totalRounds = nEven - 1;

        var result = ImmutableDictionary.CreateBuilder<int, ImmutableList<Match>>();

        // Working array for rotation
        var arr = ids.ToArray();

        for (var round = 1; round <= totalRounds; round++)
        {
            var matches = ImmutableList.CreateBuilder<Match>();
            var matchIndex = 0;

            // Pair positions: [0] vs [n-1], [1] vs [n-2], etc.
            for (var i = 0; i < nEven / 2; i++)
            {
                var idA = arr[i];
                var idB = arr[nEven - 1 - i];

                if (idA == GhostByeId)
                {
                    // Player B sits (round-robin sit, no win)
                    var matchId = $"r{round}-m{matchIndex}";
                    matches.Add(Match.CreateSit(matchId, round, idB));
                }
                else if (idB == GhostByeId)
                {
                    // Player A sits (round-robin sit, no win)
                    var matchId = $"r{round}-m{matchIndex}";
                    matches.Add(Match.CreateSit(matchId, round, idA));
                }
                else
                {
                    // Regular match
                    var matchId = $"r{round}-m{matchIndex}";
                    matches.Add(Match.Create(matchId, round, idA, idB));
                }

                matchIndex++;
            }

            result.Add(round, matches.ToImmutable());

            // Rotate: keep [0] fixed, rotate rest clockwise
            // New array = [arr[0], arr[n-1], arr[1], arr[2], ..., arr[n-2]]
            Rotate(arr);
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Generates pairings for a specific round.
    /// </summary>
    public static ImmutableList<Match> GenerateRound(EventState state, int roundNumber)
    {
        var allRounds = GenerateAllRounds(state);
        return allRounds.TryGetValue(roundNumber, out var matches)
            ? matches
            : ImmutableList<Match>.Empty;
    }

    /// <summary>
    /// Rotates the array: keep [0] fixed, rotate rest clockwise.
    /// [A, B, C, D, E] becomes [A, E, B, C, D].
    /// </summary>
    private static void Rotate(string[] arr)
    {
        if (arr.Length <= 2)
        {
            return;
        }

        var last = arr[^1];
        for (var i = arr.Length - 1; i > 1; i--)
        {
            arr[i] = arr[i - 1];
        }

        arr[1] = last;
    }
}
