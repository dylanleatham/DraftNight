namespace DraftApp.Engine.Errors;

/// <summary>
/// Round number is out of valid range.
/// </summary>
public sealed record InvalidRoundNumberError(int Round, int MaxRound)
    : EngineError($"Invalid round number: {Round}. Must be between 1 and {MaxRound}.");
