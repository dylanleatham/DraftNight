namespace DraftApp.Api.Data.Enums;

/// <summary>
/// Status of a match.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// Match not yet started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Match in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Match finalized with a winner.
    /// </summary>
    Final = 2
}
