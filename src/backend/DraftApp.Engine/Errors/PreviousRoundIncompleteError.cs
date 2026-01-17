namespace DraftApp.Engine.Errors;

/// <summary>
/// Previous round not complete.
/// </summary>
public sealed record PreviousRoundIncompleteError(int PreviousRound)
    : EngineError($"Cannot generate pairings: round {PreviousRound} is not complete.");
