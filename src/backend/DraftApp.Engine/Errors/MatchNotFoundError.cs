namespace DraftApp.Engine.Errors;

/// <summary>
/// Match not found.
/// </summary>
public sealed record MatchNotFoundError(int Round, string MatchId)
    : EngineError($"Match '{MatchId}' not found in round {Round}.");
