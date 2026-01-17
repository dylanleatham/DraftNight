namespace DraftApp.Engine.Errors;

/// <summary>
/// Player already dropped.
/// </summary>
public sealed record PlayerAlreadyDroppedError(string PlayerId)
    : EngineError($"Player '{PlayerId}' has already dropped.");
