namespace DraftApp.Engine.Errors;

/// <summary>
/// Round pairings already exist.
/// </summary>
public sealed record RoundAlreadyGeneratedError(int Round)
    : EngineError($"Pairings for round {Round} have already been generated.");
