namespace DraftApp.Engine.Errors;

/// <summary>
/// Round has no finalized non-BYE matches to reopen.
/// </summary>
public sealed record NoFinalizedMatchesError(int RoundNumber)
    : EngineError($"Round {RoundNumber} has no finalized non-BYE matches to reopen.");
