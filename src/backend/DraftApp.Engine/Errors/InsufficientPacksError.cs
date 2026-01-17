namespace DraftApp.Engine.Errors;

/// <summary>
/// Insufficient packs for prize pool (P is less than 0).
/// </summary>
public sealed record InsufficientPacksError(int PacksInBox, int DraftConsumed)
    : EngineError($"Insufficient packs: {PacksInBox} in box, {DraftConsumed} consumed for draft. Prize packs would be negative.");
