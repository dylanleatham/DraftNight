namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Prize allocation data in event snapshot.
/// </summary>
public sealed record PrizeAllocationResponse
{
    public required Guid PlayerId { get; init; }

    public required int PacksAwarded { get; init; }
}
