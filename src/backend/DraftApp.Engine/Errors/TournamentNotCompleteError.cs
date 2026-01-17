namespace DraftApp.Engine.Errors;

/// <summary>
/// Tournament not complete (cannot allocate prizes).
/// </summary>
public sealed record TournamentNotCompleteError()
    : EngineError("Cannot allocate prizes: tournament is not complete.");
