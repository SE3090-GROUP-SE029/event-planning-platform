using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestRegistrationFlow1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequirementNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EventStartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventEndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreferredLocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.CheckConstraint("CK_Events_Dates", "\"EventEndDate\" > \"EventStartDate\"");
                });

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
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSubmissions", x => x.Id);
                    table.CheckConstraint("CK_RegistrationSubmissions_Status", "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED')");
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
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "RegistrationSubmissions");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "RegistrationForms");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
