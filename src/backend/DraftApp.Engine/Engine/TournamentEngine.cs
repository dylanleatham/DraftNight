using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Public facade for the tournament engine.
/// All operations are pure functions operating on immutable state.
/// </summary>
public static class TournamentEngine
{
    /// <summary>
    /// Initializes a new tournament event.
    /// </summary>
    /// <param name="eventId">Unique event identifier.</param>
    /// <param name="players">List of (id, name) tuples in seed order.</param>
    /// <param name="packsInBox">Total packs in the booster box.</param>
    /// <returns>Initialized event state or an error.</returns>
    public static EngineResult<EventState> InitializeEvent(
        string eventId,
        IReadOnlyList<(string Id, string Name)> players,
        int packsInBox) =>
        EventInitializer.Initialize(eventId, players, packsInBox);

    /// <summary>
    /// Generates pairings for a specific round.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number to generate pairings for.</param>
    /// <returns>Updated event state with pairings or an error.</returns>
    public static EngineResult<EventState> GenerateRoundPairings(
        EventState state,
        int roundNumber) =>
        PairingGenerator.GenerateRound(state, roundNumber);

    /// <summary>
    /// Finalizes a match result.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="winnerId">Winner's player ID.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> FinalizeMatch(
        EventState state,
        int roundNumber,
        string matchId,
        string winnerId) =>
        MatchFinalizer.Finalize(state, roundNumber, matchId, winnerId);

    /// <summary>
    /// Drops a player from the tournament.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="playerId">Player ID to drop.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> DropPlayer(
        EventState state,
        string playerId) =>
        PlayerDropper.Drop(state, playerId);

    /// <summary>
    /// Allocates prize packs to players.
    /// </summary>
    /// <param name="state">Completed tournament state.</param>
    /// <returns>Updated event state with prize allocations or an error.</returns>
    public static EngineResult<EventState> AllocatePrizes(EventState state) =>
        PrizeAllocator.Allocate(state);

    /// <summary>
    /// Reopens a finalized match, reversing the result.
    /// This is a repair operation that allows correcting mistakes.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> ReopenMatch(
        EventState state,
        int roundNumber,
        string matchId) =>
        MatchReopener.Reopen(state, roundNumber, matchId);
}
