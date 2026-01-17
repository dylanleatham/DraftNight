namespace DraftApp.Engine.Errors;

/// <summary>
/// Prizes already allocated.
/// </summary>
public sealed record PrizesAlreadyAllocatedError()
    : EngineError("Prizes have already been allocated.");
