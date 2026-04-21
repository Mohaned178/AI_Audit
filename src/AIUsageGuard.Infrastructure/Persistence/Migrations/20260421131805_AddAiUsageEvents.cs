using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIUsageGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromptPreview = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    InputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    OutputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_ActorUserId_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_EventType_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_IdempotencyKey",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_events_WorkspaceId_ToolName_OccurredAt",
                table: "ai_usage_events",
                columns: new[] { "WorkspaceId", "ToolName", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_events");
        }
    }
}
