namespace DraftApp.Engine.Errors;

/// <summary>
/// Can only regenerate pairings for the current round.
/// </summary>
public sealed record CanOnlyRegenerateCurrentRoundError(int RoundNumber, int CurrentRound)
    : EngineError($"Cannot regenerate round {RoundNumber}. Can only regenerate current round ({CurrentRound}).");
