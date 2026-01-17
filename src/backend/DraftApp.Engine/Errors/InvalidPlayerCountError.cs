namespace DraftApp.Engine.Errors;

/// <summary>
/// Invalid player count (must be 2-8).
/// </summary>
public sealed record InvalidPlayerCountError(int Count)
    : EngineError($"Invalid player count: {Count}. Must be between 2 and 8.");
