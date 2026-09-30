using System.Text.Json;

namespace DraftApp.Api.Services;

/// <summary>
/// The display-relevant fields of an audit entry's AfterJson payload.
/// </summary>
internal readonly record struct AuditDetails(int? RoundNumber, Guid? WinnerId, string? PlayerName)
{
    /// <summary>
    /// Extracts known fields, ignoring anything missing or malformed so one bad row
    /// can't break the whole audit log.
    /// </summary>
    public static AuditDetails Parse(string? afterJson)
    {
        if (string.IsNullOrWhiteSpace(afterJson))
        {
            return default;
        }

        try
        {
            using var doc = JsonDocument.Parse(afterJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            int? roundNumber = root.TryGetProperty("roundNumber", out var r) && r.TryGetInt32(out var n) ? n : null;
            Guid? winnerId = root.TryGetProperty("winnerId", out var w) && w.ValueKind == JsonValueKind.String && Guid.TryParse(w.GetString(), out var g) ? g : null;
            string? playerName = root.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String ? nm.GetString() : null;

            return new AuditDetails(roundNumber, winnerId, playerName);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
