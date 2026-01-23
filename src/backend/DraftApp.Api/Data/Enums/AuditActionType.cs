namespace DraftApp.Api.Data.Enums;

/// <summary>
/// Types of auditable actions.
/// </summary>
public enum AuditActionType
{
    /// <summary>
    /// Event was created.
    /// </summary>
    EventCreated = 0,

    /// <summary>
    /// Event was started.
    /// </summary>
    EventStarted = 1,

    /// <summary>
    /// Player joined the event.
    /// </summary>
    PlayerJoined = 2,

    /// <summary>
    /// Player dropped from the event.
    /// </summary>
    PlayerDropped = 3,

    /// <summary>
    /// Round pairings were generated.
    /// </summary>
    PairingsGenerated = 4,

    /// <summary>
    /// Match result was finalized.
    /// </summary>
    MatchFinalized = 5,

    /// <summary>
    /// Prizes were allocated.
    /// </summary>
    PrizesAllocated = 6,

    /// <summary>
    /// Host performed a repair action.
    /// </summary>
    HostRepair = 7,

    /// <summary>
    /// Match was reopened for correction.
    /// </summary>
    MatchReopened = 8,

    /// <summary>
    /// Opponents were swapped between matches.
    /// </summary>
    OpponentsSwapped = 9,

    /// <summary>
    /// All matches in a round were reopened.
    /// </summary>
    RoundReopened = 10,

    /// <summary>
    /// Pairings were regenerated for a round.
    /// </summary>
    PairingsRegenerated = 11
}
