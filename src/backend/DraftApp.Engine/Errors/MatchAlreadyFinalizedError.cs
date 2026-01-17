namespace DraftApp.Engine.Errors;

/// <summary>
/// Match already has a result.
/// </summary>
public sealed record MatchAlreadyFinalizedError(string MatchId)
    : EngineError($"Match '{MatchId}' already has a result.");
