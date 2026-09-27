using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationQuestionsAndAutomatedAiDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GuestAiReviews_Result",
                table: "GuestAiReviews");

            migrationBuilder.AddColumn<int>(
                name: "RejectionDeliveryAttempts",
                table: "RegistrationSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionDeliveryStatus",
                table: "RegistrationSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectionLastAttemptAt",
                table: "RegistrationSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectionSentAt",
                table: "RegistrationSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "GuestAiReviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RegistrationSubmissions_Id_RegistrationFormId",
                table: "RegistrationSubmissions",
                columns: new[] { "Id", "RegistrationFormId" });

            migrationBuilder.CreateTable(
                name: "RegistrationQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationQuestions", x => x.Id);
                    table.UniqueConstraint("AK_RegistrationQuestions_Id_RegistrationFormId", x => new { x.Id, x.RegistrationFormId });
                    table.ForeignKey(
                        name: "FK_RegistrationQuestions_RegistrationForms_RegistrationFormId",
                        column: x => x.RegistrationFormId,
                        principalTable: "RegistrationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationAnswers",
                columns: table => new
                {
                    RegistrationSubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationAnswers", x => new { x.RegistrationSubmissionId, x.RegistrationQuestionId });
                    table.ForeignKey(
                        name: "FK_RegistrationAnswers_RegistrationQuestions_RegistrationQuest~",
                        columns: x => new { x.RegistrationQuestionId, x.RegistrationFormId },
                        principalTable: "RegistrationQuestions",
                        principalColumns: new[] { "Id", "RegistrationFormId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationAnswers_RegistrationSubmissions_RegistrationSub~",
                        columns: x => new { x.RegistrationSubmissionId, x.RegistrationFormId },
                        principalTable: "RegistrationSubmissions",
                        principalColumns: new[] { "Id", "RegistrationFormId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions",
                sql: "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED', 'PENDING_AI', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GuestAiReviews_Decision",
                table: "GuestAiReviews",
                sql: "\"Decision\" IS NULL OR \"Decision\" IN ('ACCEPTED', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GuestAiReviews_Result",
                table: "GuestAiReviews",
                sql: "(\"Status\" = 'COMPLETED' AND (\"Decision\" IS NOT NULL OR \"Recommendation\" IS NOT NULL) AND \"Confidence\" IS NOT NULL AND \"AnalyzedAt\" IS NOT NULL AND cardinality(\"Reasons\") > 0 AND \"Model\" IS NOT NULL AND \"PromptVersion\" IS NOT NULL) OR (\"Status\" <> 'COMPLETED' AND \"Decision\" IS NULL AND \"Confidence\" IS NULL AND \"AnalyzedAt\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationAnswers_RegistrationQuestionId_RegistrationForm~",
                table: "RegistrationAnswers",
                columns: new[] { "RegistrationQuestionId", "RegistrationFormId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationAnswers_RegistrationSubmissionId_RegistrationFo~",
                table: "RegistrationAnswers",
                columns: new[] { "RegistrationSubmissionId", "RegistrationFormId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationQuestions_RegistrationFormId_DisplayOrder",
                table: "RegistrationQuestions",
                columns: new[] { "RegistrationFormId", "DisplayOrder" },
                unique: true,
                filter: "\"IsSelected\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationAnswers");

            migrationBuilder.DropTable(
                name: "RegistrationQuestions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RegistrationSubmissions_Id_RegistrationFormId",
                table: "RegistrationSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GuestAiReviews_Decision",
                table: "GuestAiReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GuestAiReviews_Result",
                table: "GuestAiReviews");

            migrationBuilder.DropColumn(
                name: "RejectionDeliveryAttempts",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "RejectionDeliveryStatus",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "RejectionLastAttemptAt",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "RejectionSentAt",
                table: "RegistrationSubmissions");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "GuestAiReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions",
                sql: "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GuestAiReviews_Result",
                table: "GuestAiReviews",
                sql: "(\"Status\" = 'COMPLETED' AND \"Recommendation\" IS NOT NULL AND \"Confidence\" IS NOT NULL AND \"AnalyzedAt\" IS NOT NULL AND cardinality(\"Reasons\") > 0 AND \"Model\" IS NOT NULL AND \"PromptVersion\" IS NOT NULL) OR (\"Status\" <> 'COMPLETED' AND \"Recommendation\" IS NULL AND \"Confidence\" IS NULL AND \"AnalyzedAt\" IS NULL)");
        }
    }
}
