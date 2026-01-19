namespace DraftApp.Api.Data.Enums;

/// <summary>
/// Status of a tournament event.
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is being set up, players can join.
    /// </summary>
    Setup = 0,

    /// <summary>
    /// Event is active, rounds are being played.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Event is complete, prizes allocated.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Event is archived.
    /// </summary>
    Archived = 3
}
