namespace DraftApp.Engine.Errors;

/// <summary>
/// Match is finalized and cannot be modified.
/// </summary>
public sealed record MatchNotOpenError(string MatchId)
    : EngineError($"Match '{MatchId}' is finalized and cannot be modified.");
