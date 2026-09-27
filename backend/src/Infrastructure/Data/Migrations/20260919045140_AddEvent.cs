using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF to_regclass('"Events"') IS NULL THEN
                        CREATE TABLE "Events" (
                            "Id" uuid NOT NULL,
                            "OwnerId" uuid NOT NULL,
                            "EventName" character varying(200) NOT NULL,
                            "EventType" integer NOT NULL,
                            "GuestCount" integer NOT NULL,
                            "Budget" numeric(18,2) NOT NULL,
                            "PreferredVenue" character varying(500),
                            "PreferredDate" timestamp with time zone NOT NULL,
                            "EventDuration" interval NOT NULL,
                            "Requirements" character varying(4000),
                            "Status" integer NOT NULL DEFAULT 0,
                            "CreatedAt" timestamp with time zone NOT NULL,
                            "UpdatedAt" timestamp with time zone,
                            CONSTRAINT "PK_Events" PRIMARY KEY ("Id"),
                            CONSTRAINT "CK_Events_Budget_NonNegative" CHECK ("Budget" >= 0),
                            CONSTRAINT "CK_Events_GuestCount_Positive" CHECK ("GuestCount" > 0)
                        );
                        CREATE INDEX "IX_Events_OwnerId" ON "Events" ("OwnerId");
                    END IF;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
