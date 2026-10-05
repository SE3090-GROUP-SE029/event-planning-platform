using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorRecommendationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "VendorRecommendationRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureMessage",
                table: "VendorRecommendationRuns",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "VendorRecommendationRuns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "VendorRecommendationRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "VendorRecommendationRuns",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorRecommendationRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "VendorRecommendationRuns"
                SET "Status" = 'Completed',
                    "Stage" = 'Completed',
                    "UpdatedAt" = "CreatedAt",
                    "CompletedAt" = "CreatedAt";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Stage",
                table: "VendorRecommendationRuns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorRecommendationRuns",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorRecommendationRuns",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorRecommendationRuns_EventId",
                table: "VendorRecommendationRuns",
                column: "EventId",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Running')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorRecommendationRuns_EventId",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "FailureMessage",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VendorRecommendationRuns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "VendorRecommendationRuns");
        }
    }
}
