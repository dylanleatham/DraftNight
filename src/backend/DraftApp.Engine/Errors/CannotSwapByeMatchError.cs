namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot swap opponents in a BYE or sit match.
/// </summary>
public sealed record CannotSwapByeMatchError(string MatchId)
    : EngineError($"Cannot swap opponents in BYE or sit match '{MatchId}'.");
