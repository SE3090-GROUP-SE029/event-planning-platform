using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class SeparateGuestLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationSubmissions_GuestId_EventId",
                table: "RegistrationSubmissions");
            migrationBuilder.DropCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions");
            migrationBuilder.DropCheckConstraint(
                name: "CK_GuestAiReviews_Status",
                table: "GuestAiReviews");

            migrationBuilder.AddColumn<string>(
                name: "ReviewDecision",
                table: "RegistrationSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ReviewSource",
                table: "RegistrationSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "RegistrationSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestCheckIns",
                columns: table => new
                {
                    RegistrationSubmissionId = table.Column<long>(type: "bigint", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestCheckIns", x => x.RegistrationSubmissionId);
                    table.ForeignKey(
                        name: "FK_GuestCheckIns_RegistrationSubmissions_RegistrationSubmissionId",
                        column: x => x.RegistrationSubmissionId,
                        principalTable: "RegistrationSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO "GuestCheckIns" ("RegistrationSubmissionId", "CheckedInAt", "Method")
                SELECT "Id", "CheckedInAt", COALESCE("CheckedInMethod", 'QR_CODE')
                FROM "RegistrationSubmissions"
                WHERE "CheckedInAt" IS NOT NULL;

                UPDATE "RegistrationSubmissions" AS registration
                SET "ReviewDecision" = CASE
                        WHEN registration."Status" IN ('CONFIRMED', 'WAITING_LIST') THEN 'ACCEPTED'
                        WHEN registration."Status" = 'REJECTED' THEN 'REJECTED'
                        ELSE NULL
                    END,
                    "ReviewSource" = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM "GuestAiReviews" AS review
                            WHERE review."RegistrationSubmissionId" = registration."Id"
                              AND review."Status" = 'COMPLETED'
                              AND review."Decision" = CASE
                                  WHEN registration."Status" IN ('CONFIRMED', 'WAITING_LIST') THEN 'ACCEPTED'
                                  WHEN registration."Status" = 'REJECTED' THEN 'REJECTED'
                                  ELSE NULL
                              END
                        ) THEN 'AI'
                        ELSE NULL
                    END,
                    "ReviewedAt" = CASE
                        WHEN registration."Status" IN ('CONFIRMED', 'WAITING_LIST', 'REJECTED')
                            THEN COALESCE((
                                SELECT review."AnalyzedAt"
                                FROM "GuestAiReviews" AS review
                                WHERE review."RegistrationSubmissionId" = registration."Id"
                            ), registration."UpdatedAt")
                        ELSE NULL
                    END
                ;

                UPDATE "RegistrationSubmissions"
                SET "Status" = CASE "Status"
                    WHEN 'PENDING_AI' THEN 'PENDING_REVIEW'
                    WHEN 'WAITING_LIST' THEN 'WAITLISTED'
                    ELSE "Status"
                END;
                """);

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "RegistrationSubmissions");
            migrationBuilder.DropColumn(
                name: "CheckedInMethod",
                table: "RegistrationSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_GuestId_EventId",
                table: "RegistrationSubmissions",
                columns: new[] { "GuestId", "EventId" },
                unique: true);
            migrationBuilder.AddCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions",
                sql: "\"Status\" IN ('PENDING_REVIEW', 'ACCEPTED', 'REJECTED', 'CONFIRMED', 'WAITLISTED', 'CANCELLED')");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GuestAiReviews_Status",
                table: "GuestAiReviews",
                sql: "\"Status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED', 'SUPERSEDED')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationSubmissions_GuestId_EventId",
                table: "RegistrationSubmissions");
            migrationBuilder.DropCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions");
            migrationBuilder.DropCheckConstraint(
                name: "CK_GuestAiReviews_Status",
                table: "GuestAiReviews");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                table: "RegistrationSubmissions",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CheckedInMethod",
                table: "RegistrationSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "RegistrationSubmissions" AS registration
                SET "CheckedInAt" = check_in."CheckedInAt",
                    "CheckedInMethod" = check_in."Method"
                FROM "GuestCheckIns" AS check_in
                WHERE check_in."RegistrationSubmissionId" = registration."Id";

                UPDATE "RegistrationSubmissions"
                SET "Status" = CASE "Status"
                    WHEN 'PENDING_REVIEW' THEN 'PENDING_AI'
                    WHEN 'ACCEPTED' THEN 'PENDING_AI'
                    WHEN 'WAITLISTED' THEN 'WAITING_LIST'
                    ELSE "Status"
                END;
                """);

            migrationBuilder.DropTable(name: "GuestCheckIns");
            migrationBuilder.DropColumn(name: "ReviewDecision", table: "RegistrationSubmissions");
            migrationBuilder.DropColumn(name: "ReviewSource", table: "RegistrationSubmissions");
            migrationBuilder.DropColumn(name: "ReviewedAt", table: "RegistrationSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSubmissions_GuestId_EventId",
                table: "RegistrationSubmissions",
                columns: new[] { "GuestId", "EventId" });
            migrationBuilder.AddCheckConstraint(
                name: "CK_RegistrationSubmissions_Status",
                table: "RegistrationSubmissions",
                sql: "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED', 'PENDING_AI', 'REJECTED')");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GuestAiReviews_Status",
                table: "GuestAiReviews",
                sql: "\"Status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
        }
    }
}
