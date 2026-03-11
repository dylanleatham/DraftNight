namespace DraftApp.Engine.Errors;

/// <summary>
/// A player name is empty or whitespace.
/// </summary>
public sealed record EmptyPlayerNameError(string PlayerId)
    : EngineError($"Player '{PlayerId}' has an empty or whitespace name");
