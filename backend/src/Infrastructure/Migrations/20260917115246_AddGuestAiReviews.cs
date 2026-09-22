using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestAiReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestAiReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationSubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    Reasons = table.Column<string[]>(type: "text[]", nullable: false),
                    Flags = table.Column<string[]>(type: "text[]", nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestAiReviews", x => x.Id);
                    table.CheckConstraint("CK_GuestAiReviews_Confidence", "\"Confidence\" IS NULL OR (\"Confidence\" >= 0 AND \"Confidence\" <= 1)");
                    table.CheckConstraint("CK_GuestAiReviews_Lease", "\"Status\" <> 'PROCESSING' OR (\"AttemptId\" IS NOT NULL AND \"LeaseExpiresAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_GuestAiReviews_Recommendation", "\"Recommendation\" IS NULL OR \"Recommendation\" IN ('ELIGIBLE', 'REVIEW', 'REJECTED')");
                    table.CheckConstraint("CK_GuestAiReviews_Result", "(\"Status\" = 'COMPLETED' AND \"Recommendation\" IS NOT NULL AND \"Confidence\" IS NOT NULL AND \"AnalyzedAt\" IS NOT NULL AND cardinality(\"Reasons\") > 0 AND \"Model\" IS NOT NULL AND \"PromptVersion\" IS NOT NULL) OR (\"Status\" <> 'COMPLETED' AND \"Recommendation\" IS NULL AND \"Confidence\" IS NULL AND \"AnalyzedAt\" IS NULL)");
                    table.CheckConstraint("CK_GuestAiReviews_Status", "\"Status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_GuestAiReviews_RegistrationSubmissions_RegistrationSubmissi~",
                        column: x => x.RegistrationSubmissionId,
                        principalTable: "RegistrationSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestAiReviews_RegistrationSubmissionId",
                table: "GuestAiReviews",
                column: "RegistrationSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestAiReviews_Status_RequestedAt",
                table: "GuestAiReviews",
                columns: new[] { "Status", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestAiReviews");
        }
    }
}
