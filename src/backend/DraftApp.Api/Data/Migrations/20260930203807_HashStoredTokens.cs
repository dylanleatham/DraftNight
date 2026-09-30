using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftApp.Api.Data.Migrations;

/// <inheritdoc />
public partial class HashStoredTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Tokens used to be stored as issued. Replace each with the lowercase hex SHA-256
        // of its UTF-8 bytes, matching AuthorizationService.HashToken. Tokens are
        // base64url (ASCII), so the varchar cast yields the same bytes as UTF-8.
        migrationBuilder.Sql(
            "UPDATE Events SET HostToken = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CAST(HostToken AS varchar(100))), 2));");
        migrationBuilder.Sql(
            "UPDATE Players SET PlayerToken = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CAST(PlayerToken AS varchar(100))), 2)) WHERE PlayerToken IS NOT NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Hashing is one-way; existing sessions can be recovered with the resume endpoint.
    }
}
