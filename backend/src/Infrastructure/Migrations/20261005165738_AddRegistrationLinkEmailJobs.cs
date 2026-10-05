using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationLinkEmailJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationLinkEmailJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationLinkEmailJobs", x => x.Id);
                    table.CheckConstraint("CK_RegistrationLinkEmailJobs_AttemptCount", "\"AttemptCount\" BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_RegistrationLinkEmailJobs_Lease", "(\"Status\" = 'PROCESSING' AND \"LockedUntil\" IS NOT NULL) OR (\"Status\" <> 'PROCESSING' AND \"LockedUntil\" IS NULL)");
                    table.CheckConstraint("CK_RegistrationLinkEmailJobs_Status", "\"Status\" IN ('QUEUED', 'PROCESSING', 'SENT', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_RegistrationLinkEmailJobs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationLinkEmailJobs_EventId",
                table: "RegistrationLinkEmailJobs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationLinkEmailJobs_Status_NextAttemptAt_CreatedAt",
                table: "RegistrationLinkEmailJobs",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationLinkEmailJobs");
        }
    }
}
