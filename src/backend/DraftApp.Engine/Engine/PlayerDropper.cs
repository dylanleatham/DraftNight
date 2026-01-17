using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles player drop logic.
/// </summary>
internal static class PlayerDropper
{
    /// <summary>
    /// Drops a player from the tournament.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="playerId">Player ID to drop.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> Drop(EventState state, string playerId)
    {
        // Check if tournament is complete
        if (state.IsComplete)
        {
            return EngineResult<EventState>.Fail(new TournamentCompleteError());
        }

        // Check if player exists
        if (!state.Players.TryGetValue(playerId, out var player))
        {
            return EngineResult<EventState>.Fail(new PlayerNotFoundError(playerId));
        }

        // Check if already dropped
        if (player.IsDropped)
        {
            return EngineResult<EventState>.Fail(new PlayerAlreadyDroppedError(playerId));
        }

        // Mark player as dropped
        var updatedPlayer = player.WithDropped();
        var newState = state.WithPlayer(updatedPlayer);

        return EngineResult<EventState>.Ok(newState);
    }
}
