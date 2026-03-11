namespace DraftApp.Api.Models;

/// <summary>
/// Well-known error strings returned by mutation operations.
/// Used by both the service layer and controller to avoid magic strings.
/// </summary>
public static class MutationErrors
{
    public const string EventNotFound = "Event not found";
    public const string VersionConflict = "Version conflict";
}
