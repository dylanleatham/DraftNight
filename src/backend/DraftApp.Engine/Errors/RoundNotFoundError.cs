namespace DraftApp.Engine.Errors;

/// <summary>
/// Round does not exist or has no pairings.
/// </summary>
public sealed record RoundNotFoundError(int RoundNumber)
    : EngineError($"Round {RoundNumber} does not exist or has no pairings.");
