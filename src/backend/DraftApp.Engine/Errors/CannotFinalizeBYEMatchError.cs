namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot finalize BYE match (auto-finalized).
/// </summary>
public sealed record CannotFinalizeBYEMatchError(string MatchId)
    : EngineError($"Cannot manually finalize BYE match '{MatchId}'. BYE matches are auto-finalized.");
