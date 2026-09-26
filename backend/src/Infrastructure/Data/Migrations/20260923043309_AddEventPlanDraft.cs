using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPlanDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventPlanDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceCategories = table.Column<string>(type: "jsonb", nullable: false),
                    BudgetAllocation = table.Column<string>(type: "jsonb", nullable: false),
                    TargetVendorTypes = table.Column<string>(type: "jsonb", nullable: false),
                    ProposedTimeline = table.Column<string>(type: "jsonb", nullable: false),
                    Rationale = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    PlanCompletenessScore = table.Column<int>(type: "integer", nullable: false),
                    ValidationSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    EventSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    PlannerDecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannerRemarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedById = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAuditAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdentifiedRisks = table.Column<string>(type: "jsonb", nullable: true),
                    MissingRequirements = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPlanDrafts", x => x.Id);
                    table.CheckConstraint("CK_EventPlanDrafts_PlanCompletenessScore_Range", "\"PlanCompletenessScore\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_EventPlanDrafts_Version_Positive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_EventPlanDrafts_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventPlanDrafts_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventPlanDrafts_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventPlanDrafts_Users_RejectedById",
                        column: x => x.RejectedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanDrafts_ApprovedById",
                table: "EventPlanDrafts",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanDrafts_CreatedById",
                table: "EventPlanDrafts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanDrafts_EventId_Status",
                table: "EventPlanDrafts",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanDrafts_EventId_Version",
                table: "EventPlanDrafts",
                columns: new[] { "EventId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventPlanDrafts_RejectedById",
                table: "EventPlanDrafts",
                column: "RejectedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventPlanDrafts");
        }
    }
}
