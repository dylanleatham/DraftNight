using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftApp.Api.Data.Migrations;

/// <inheritdoc />
public partial class PendingChanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PinHash",
            table: "Players",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PlayerToken",
            table: "Players",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "HostPinHash",
            table: "Events",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "HostToken",
            table: "Events",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.CreateIndex(
            name: "IX_Players_PlayerToken",
            table: "Players",
            column: "PlayerToken",
            unique: true,
            filter: "[PlayerToken] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Events_HostToken",
            table: "Events",
            column: "HostToken",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Players_PlayerToken",
            table: "Players");

        migrationBuilder.DropIndex(
            name: "IX_Events_HostToken",
            table: "Events");

        migrationBuilder.DropColumn(
            name: "PinHash",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "PlayerToken",
            table: "Players");

        migrationBuilder.DropColumn(
            name: "HostPinHash",
            table: "Events");

        migrationBuilder.DropColumn(
            name: "HostToken",
            table: "Events");
    }
}
