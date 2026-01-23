namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot swap opponents within the same match.
/// </summary>
public sealed record SameMatchSwapError()
    : EngineError("Cannot swap opponents within the same match. Use different matches.");
