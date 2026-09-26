using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class C2_Quotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedStartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedEndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    QuotedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    VendorTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.CheckConstraint("CK_Quotations_DateRange", "\"RequestedStartDateTime\" < \"RequestedEndDateTime\"");
                    table.CheckConstraint("CK_Quotations_QuotedPrice_NonNegative", "\"QuotedPrice\" IS NULL OR \"QuotedPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_Quotations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_VendorServices_VendorServiceId",
                        column: x => x.VendorServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_EventId",
                table: "Quotations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_RequestedByUserId",
                table: "Quotations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Status",
                table: "Quotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_VendorId",
                table: "Quotations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_VendorServiceId",
                table: "Quotations",
                column: "VendorServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotations");
        }
    }
}
