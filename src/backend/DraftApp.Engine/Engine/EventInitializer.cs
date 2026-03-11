using System.Collections.Immutable;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles tournament event initialization.
/// </summary>
internal static class EventInitializer
{
    /// <summary>
    /// Minimum allowed player count.
    /// </summary>
    public const int MinPlayers = 2;

    /// <summary>
    /// Maximum allowed player count.
    /// </summary>
    public const int MaxPlayers = 8;

    /// <summary>
    /// Packs consumed per player during draft.
    /// </summary>
    public const int PacksPerPlayer = 3;

    /// <summary>
    /// Player count threshold for Swiss format (N >= this uses Swiss).
    /// </summary>
    public const int SwissThreshold = 5;

    /// <summary>
    /// Number of rounds for Swiss format.
    /// </summary>
    public const int SwissRounds = 3;

    /// <summary>
    /// Initializes a new tournament event.
    /// </summary>
    /// <param name="eventId">Unique event identifier.</param>
    /// <param name="players">List of (id, name) tuples in seed order.</param>
    /// <param name="packsInBox">Total packs in the booster box.</param>
    /// <returns>Initialized event state or an error.</returns>
    public static EngineResult<EventState> Initialize(
        string eventId,
        IReadOnlyList<(string Id, string Name)> players,
        int packsInBox)
    {
        // Validate player count
        if (players.Count < MinPlayers || players.Count > MaxPlayers)
        {
            return EngineResult<EventState>.Fail(new InvalidPlayerCountError(players.Count));
        }

        // Validate player names are non-empty
        var emptyNamePlayer = players.FirstOrDefault(p => string.IsNullOrWhiteSpace(p.Name));
        if (emptyNamePlayer != default)
        {
            return EngineResult<EventState>.Fail(new EmptyPlayerNameError(emptyNamePlayer.Id));
        }

        // Check for duplicate IDs
        var duplicateId = players
            .GroupBy(p => p.Id)
            .FirstOrDefault(g => g.Count() > 1)?.Key;

        if (duplicateId is not null)
        {
            return EngineResult<EventState>.Fail(new DuplicatePlayerIdError(duplicateId));
        }

        // Calculate prize packs
        var draftConsumed = PacksPerPlayer * players.Count;
        var prizePacks = packsInBox - draftConsumed;

        if (prizePacks < 0)
        {
            return EngineResult<EventState>.Fail(
                new InsufficientPacksError(packsInBox, draftConsumed));
        }

        // Determine format and round count
        var format = DetermineFormat(players.Count);
        var totalRounds = CalculateTotalRounds(players.Count, format);

        // Create player records
        var playersDict = players
            .Select((p, index) => Player.Create(p.Id, p.Name, index + 1))
            .ToImmutableDictionary(p => p.Id);

        var state = new EventState
        {
            EventId = eventId,
            Players = playersDict,
            PacksInBox = packsInBox,
            PrizePacks = prizePacks,
            Format = format,
            TotalRounds = totalRounds
        };

        return EngineResult<EventState>.Ok(state);
    }

    /// <summary>
    /// Determines the tournament format based on player count.
    /// </summary>
    public static TournamentFormat DetermineFormat(int playerCount) =>
        playerCount >= SwissThreshold ? TournamentFormat.Swiss : TournamentFormat.RoundRobin;

    /// <summary>
    /// Calculates the total number of rounds.
    /// </summary>
    public static int CalculateTotalRounds(int playerCount, TournamentFormat format)
    {
        if (format == TournamentFormat.Swiss)
        {
            return SwissRounds;
        }

        // Round-robin: N-1 for even N, N for odd N (ghost BYE)
        return playerCount % 2 == 0 ? playerCount - 1 : playerCount;
    }
}
