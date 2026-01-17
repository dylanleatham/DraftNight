namespace DraftApp.Engine.Errors;

/// <summary>
/// Tournament already complete.
/// </summary>
public sealed record TournamentCompleteError()
    : EngineError("Tournament is already complete.");
