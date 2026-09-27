using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TempCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TimelineActivities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Guests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Organisation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                    table.UniqueConstraint("AK_Guests_Id_EventId", x => new { x.Id, x.EventId });
                    table.ForeignKey(
                        name: "FK_Guests_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpensAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SeatLimit = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublicId = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationForms", x => x.Id);
                    table.UniqueConstraint("AK_RegistrationForms_Id_EventId", x => new { x.Id, x.EventId });
                    table.CheckConstraint("CK_RegistrationForms_Period", "\"OpensAt\" < \"ClosesAt\"");
                    table.CheckConstraint("CK_RegistrationForms_Publication", "(\"Status\" = 'DRAFT' AND \"PublicId\" IS NULL AND \"PublishedAt\" IS NULL) OR (\"Status\" = 'PUBLISHED' AND \"PublicId\" IS NOT NULL AND \"PublishedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_RegistrationForms_SeatLimit", "\"SeatLimit\" > 0");
                    table.ForeignKey(
                        name: "FK_RegistrationForms_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestMessages", x => x.Id);
                });

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
                name: "RegistrationSubmissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublicReference = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    StatusSecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionDeliveryStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RejectionDeliveryAttempts = table.Column<int>(type: "integer", nullable: false),
                    RejectionLastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSubmissions", x => x.Id);
                    table.UniqueConstraint("AK_RegistrationSubmissions_Id_RegistrationFormId", x => new { x.Id, x.RegistrationFormId });
                    table.CheckConstraint("CK_RegistrationSubmissions_Status", "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED', 'PENDING_AI', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_RegistrationSubmissions_Guests_GuestId_EventId",
                        columns: x => new { x.GuestId, x.EventId },
                        principalTable: "Guests",
                        principalColumns: new[] { "Id", "EventId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationSubmissions_RegistrationForms_RegistrationFormI~",
                        columns: x => new { x.RegistrationFormId, x.EventId },
                        principalTable: "RegistrationForms",
                        principalColumns: new[] { "Id", "EventId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuestAiReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationSubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.CheckConstraint("CK_GuestAiReviews_Decision", "\"Decision\" IS NULL OR \"Decision\" IN ('ACCEPTED', 'REJECTED')");
                    table.CheckConstraint("CK_GuestAiReviews_Lease", "\"Status\" <> 'PROCESSING' OR (\"AttemptId\" IS NOT NULL AND \"LeaseExpiresAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_GuestAiReviews_Recommendation", "\"Recommendation\" IS NULL OR \"Recommendation\" IN ('ELIGIBLE', 'REVIEW', 'REJECTED')");
                    table.CheckConstraint("CK_GuestAiReviews_Result", "(\"Status\" = 'COMPLETED' AND (\"Decision\" IS NOT NULL OR \"Recommendation\" IS NOT NULL) AND \"Confidence\" IS NOT NULL AND \"AnalyzedAt\" IS NOT NULL AND cardinality(\"Reasons\") > 0 AND \"Model\" IS NOT NULL AND \"PromptVersion\" IS NOT NULL) OR (\"Status\" <> 'COMPLETED' AND \"Decision\" IS NULL AND \"Confidence\" IS NULL AND \"AnalyzedAt\" IS NULL)");
                    table.CheckConstraint("CK_GuestAiReviews_Status", "\"Status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_GuestAiReviews_RegistrationSubmissions_RegistrationSubmissi~",
                        column: x => x.RegistrationSubmissionId,
                        principalTable: "RegistrationSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationSubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RsvpStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RsvpedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_RegistrationSubmissions_RegistrationSubmissionId",
                        column: x => x.RegistrationSubmissionId,
                        principalTable: "RegistrationSubmissions",
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

            migrationBuilder.CreateIndex(
                name: "IX_GuestAiReviews_RegistrationSubmissionId",
                table: "GuestAiReviews",
                column: "RegistrationSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestAiReviews_Status_RequestedAt",
                table: "GuestAiReviews",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_EventId_NormalizedEmail",
                table: "Guests",
                columns: new[] { "EventId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_RegistrationSubmissionId",
                table: "Invitations",
                column: "RegistrationSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationAnswers_RegistrationQuestionId_RegistrationForm~",
                table: "RegistrationAnswers",
                columns: new[] { "RegistrationQuestionId", "RegistrationFormId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationAnswers_RegistrationSubmissionId_RegistrationFo~",
                table: "RegistrationAnswers",
                columns: new[] { "RegistrationSubmissionId", "RegistrationFormId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationForms_EventId",
                table: "RegistrationForms",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationForms_PublicId",
                table: "RegistrationForms",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationQuestions_RegistrationFormId_DisplayOrder",
                table: "RegistrationQuestions",
                columns: new[] { "RegistrationFormId", "DisplayOrder" },
                unique: true,
                filter: "\"IsSelected\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_EventId_Status_RegisteredAt_Id",
                table: "RegistrationSubmissions",
                columns: new[] { "EventId", "Status", "RegisteredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_GuestId",
                table: "RegistrationSubmissions",
                column: "GuestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_GuestId_EventId",
                table: "RegistrationSubmissions",
                columns: new[] { "GuestId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_PublicReference",
                table: "RegistrationSubmissions",
                column: "PublicReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_RegistrationFormId_EventId",
                table: "RegistrationSubmissions",
                columns: new[] { "RegistrationFormId", "EventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestAiReviews");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "RegistrationAnswers");

            migrationBuilder.DropTable(
                name: "TestMessages");

            migrationBuilder.DropTable(
                name: "RegistrationQuestions");

            migrationBuilder.DropTable(
                name: "RegistrationSubmissions");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "RegistrationForms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TimelineActivities");
        }
    }
}
