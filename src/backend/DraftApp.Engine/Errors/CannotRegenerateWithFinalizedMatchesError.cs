namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot regenerate pairings when non-BYE matches are already finalized.
/// </summary>
public sealed record CannotRegenerateWithFinalizedMatchesError(int RoundNumber)
    : EngineError($"Cannot regenerate pairings for round {RoundNumber} because non-BYE matches are already finalized.");
