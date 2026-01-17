namespace DraftApp.Engine.Errors;

/// <summary>
/// Duplicate player ID detected.
/// </summary>
public sealed record DuplicatePlayerIdError(string PlayerId)
    : EngineError($"Duplicate player ID: {PlayerId}");
