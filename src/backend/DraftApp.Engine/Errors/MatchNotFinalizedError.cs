namespace DraftApp.Engine.Errors;

/// <summary>
/// Match has not been finalized.
/// </summary>
public sealed record MatchNotFinalizedError(string MatchId)
    : EngineError($"Match '{MatchId}' has not been finalized.");
