namespace DraftApp.Engine.Errors;

/// <summary>
/// Player is not a participant in the specified match.
/// </summary>
public sealed record PlayerNotInMatchError(string PlayerId, string MatchId)
    : EngineError($"Player '{PlayerId}' is not in match '{MatchId}'.");
