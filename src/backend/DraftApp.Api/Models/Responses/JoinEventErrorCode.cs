namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Error codes for join event failures.
/// </summary>
public enum JoinEventErrorCode
{
    NotFound,
    LobbyFull,
    VersionConflict
}
