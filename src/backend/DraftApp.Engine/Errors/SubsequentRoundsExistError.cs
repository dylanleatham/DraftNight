namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot modify a round when subsequent rounds already exist.
/// </summary>
public sealed record SubsequentRoundsExistError(int RoundNumber)
    : EngineError($"Cannot modify round {RoundNumber} because subsequent rounds already exist.");
