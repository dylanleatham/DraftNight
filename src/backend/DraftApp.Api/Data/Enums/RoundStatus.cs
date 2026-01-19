namespace DraftApp.Api.Data.Enums;

/// <summary>
/// Status of a tournament round.
/// </summary>
public enum RoundStatus
{
    /// <summary>
    /// Round pairings not yet generated.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Pairings published, matches in progress.
    /// </summary>
    PairingsPublished = 1,

    /// <summary>
    /// All matches complete, round closed.
    /// </summary>
    Closed = 2
}
