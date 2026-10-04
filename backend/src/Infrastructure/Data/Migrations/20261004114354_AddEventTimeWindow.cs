using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTimeWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Events",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(9, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Events",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(17, 0, 0));

            migrationBuilder.Sql("""
                UPDATE "Events"
                SET "EndTime" = CASE
                    WHEN "EventDuration" > interval '0'
                         AND "EventDuration" <= interval '14 hours'
                    THEN TIME '09:00:00' + "EventDuration"
                    ELSE TIME '17:00:00'
                END;

                UPDATE "Events"
                SET "EventDuration" = "EndTime" - "StartTime";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_TimeWindow",
                table: "Events",
                sql: "\"EndTime\" > \"StartTime\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_TimeWindow",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Events");
        }
    }
}
