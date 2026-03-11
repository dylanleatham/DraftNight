namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Result of a join event operation, distinguishing between not-found and lobby-full errors.
/// </summary>
public sealed record JoinEventResult
{
    /// <summary>
    /// Gets the response if the join succeeded.
    /// </summary>
    public JoinEventResponse? Response { get; init; }

    /// <summary>
    /// Gets the error code if the join failed.
    /// </summary>
    public JoinEventErrorCode? ErrorCode { get; init; }

    public static JoinEventResult Success(JoinEventResponse response) =>
        new() { Response = response };

    public static JoinEventResult NotFound() =>
        new() { ErrorCode = JoinEventErrorCode.NotFound };

    public static JoinEventResult LobbyFull() =>
        new() { ErrorCode = JoinEventErrorCode.LobbyFull };

    public static JoinEventResult Conflict() =>
        new() { ErrorCode = JoinEventErrorCode.VersionConflict };
}
