using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftApp.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                JoinCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                PacksInBox = table.Column<int>(type: "int", nullable: false),
                PrizePacks = table.Column<int>(type: "int", nullable: false),
                Format = table.Column<int>(type: "int", nullable: false),
                TotalRounds = table.Column<int>(type: "int", nullable: false),
                PrizesAllocated = table.Column<bool>(type: "bit", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActionType = table.Column<int>(type: "int", nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuditLogs_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Seed = table.Column<int>(type: "int", nullable: false),
                MatchWins = table.Column<int>(type: "int", nullable: false),
                MatchLosses = table.Column<int>(type: "int", nullable: false),
                ByeReceived = table.Column<bool>(type: "bit", nullable: false),
                IsDropped = table.Column<bool>(type: "bit", nullable: false),
                OpponentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                LastPlayedRoundJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.Id);
                table.ForeignKey(
                    name: "FK_Players_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Rounds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoundNumber = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rounds", x => x.Id);
                table.ForeignKey(
                    name: "FK_Rounds_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PrizeAllocations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PacksAwarded = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PrizeAllocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_PrizeAllocations_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PrizeAllocations_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Matches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MatchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RoundNumber = table.Column<int>(type: "int", nullable: false),
                PlayerAId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlayerBId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Matches", x => x.Id);
                table.ForeignKey(
                    name: "FK_Matches_Players_PlayerAId",
                    column: x => x.PlayerAId,
                    principalTable: "Players",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Matches_Players_PlayerBId",
                    column: x => x.PlayerBId,
                    principalTable: "Players",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Matches_Players_WinnerId",
                    column: x => x.WinnerId,
                    principalTable: "Players",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Matches_Rounds_RoundId",
                    column: x => x.RoundId,
                    principalTable: "Rounds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EventId_CreatedAt",
            table: "AuditLogs",
            columns: new[] { "EventId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Events_JoinCode",
            table: "Events",
            column: "JoinCode",
            unique: true,
            filter: "[JoinCode] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_PlayerAId",
            table: "Matches",
            column: "PlayerAId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_PlayerBId",
            table: "Matches",
            column: "PlayerBId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_RoundId_MatchCode",
            table: "Matches",
            columns: new[] { "RoundId", "MatchCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Matches_WinnerId",
            table: "Matches",
            column: "WinnerId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_EventId_Seed",
            table: "Players",
            columns: new[] { "EventId", "Seed" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PrizeAllocations_EventId_PlayerId",
            table: "PrizeAllocations",
            columns: new[] { "EventId", "PlayerId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PrizeAllocations_PlayerId",
            table: "PrizeAllocations",
            column: "PlayerId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Rounds_EventId_RoundNumber",
            table: "Rounds",
            columns: new[] { "EventId", "RoundNumber" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs");

        migrationBuilder.DropTable(
            name: "Matches");

        migrationBuilder.DropTable(
            name: "PrizeAllocations");

        migrationBuilder.DropTable(
            name: "Rounds");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropTable(
            name: "Events");
    }
}
