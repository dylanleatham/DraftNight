namespace DraftApp.Engine.Errors;

/// <summary>
/// Player not found.
/// </summary>
public sealed record PlayerNotFoundError(string PlayerId)
    : EngineError($"Player '{PlayerId}' not found.");
