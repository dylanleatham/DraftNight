namespace DraftApp.Engine.Errors;

/// <summary>
/// Winner is not a participant in the match.
/// </summary>
public sealed record InvalidWinnerError(string MatchId, string WinnerId)
    : EngineError($"Player '{WinnerId}' is not a participant in match '{MatchId}'.");
