namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot perform operation on a dropped player.
/// </summary>
public sealed record PlayerDroppedError(string PlayerId)
    : EngineError($"Player '{PlayerId}' has been dropped from the tournament.");
