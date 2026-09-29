using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260928190000_C2_VendorRecommendations")]
    public partial class C2_VendorRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorRecommendationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventPlanDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorRecommendationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorRecommendationRuns_EventPlanDrafts_EventPlanDraftId",
                        column: x => x.EventPlanDraftId,
                        principalTable: "EventPlanDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorRecommendationRuns_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorRecommendationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PricingType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityMatch = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorRecommendationItems", x => x.Id);
                    table.CheckConstraint("CK_VendorRecommendationItems_Rank_Positive", "\"Rank\" >= 1");
                    table.CheckConstraint("CK_VendorRecommendationItems_Score_Range", "\"Score\" >= 0 AND \"Score\" <= 100");
                    table.ForeignKey(
                        name: "FK_VendorRecommendationItems_VendorRecommendationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "VendorRecommendationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorRecommendationItems_VendorServices_VendorServiceId",
                        column: x => x.VendorServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorRecommendationItems_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationItems_RunId",
                table: "VendorRecommendationItems",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationItems_VendorId",
                table: "VendorRecommendationItems",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationItems_VendorServiceId",
                table: "VendorRecommendationItems",
                column: "VendorServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationRuns_EventId_CreatedAt",
                table: "VendorRecommendationRuns",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationRuns_EventPlanDraftId",
                table: "VendorRecommendationRuns",
                column: "EventPlanDraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorRecommendationItems");

            migrationBuilder.DropTable(
                name: "VendorRecommendationRuns");
        }
    }
}
