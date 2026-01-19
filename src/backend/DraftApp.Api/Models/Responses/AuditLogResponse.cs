namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Audit log response.
/// </summary>
public sealed record AuditLogResponse
{
    public required IReadOnlyList<AuditLogEntry> Entries { get; init; }
}
