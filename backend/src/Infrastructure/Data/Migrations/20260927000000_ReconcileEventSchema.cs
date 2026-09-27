using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations;

public partial class ReconcileEventSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "TimelineActivities",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            ALTER TABLE "Events"
                ADD COLUMN IF NOT EXISTS "OwnerId" uuid,
                ADD COLUMN IF NOT EXISTS "EventName" character varying(200),
                ADD COLUMN IF NOT EXISTS "EventType" integer,
                ADD COLUMN IF NOT EXISTS "GuestCount" integer,
                ADD COLUMN IF NOT EXISTS "Budget" numeric(18,2),
                ADD COLUMN IF NOT EXISTS "PreferredVenue" character varying(500),
                ADD COLUMN IF NOT EXISTS "PreferredDate" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "EventDuration" interval,
                ADD COLUMN IF NOT EXISTS "Requirements" character varying(4000),
                ADD COLUMN IF NOT EXISTS "Status" integer,
                ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone,
                ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone;

            DO $migration$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'CreatedByUserId'
                ) THEN
                    IF EXISTS (
                        SELECT 1 FROM "Events"
                        WHERE "CreatedByUserId" IS NULL
                           OR btrim("CreatedByUserId") !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                    ) THEN
                        RAISE EXCEPTION 'Cannot reconcile Events: CreatedByUserId contains values that are not UUIDs; map these owners before retrying.';
                    END IF;
                    UPDATE "Events"
                    SET "OwnerId" = "CreatedByUserId"::uuid
                    WHERE "OwnerId" IS NULL;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'EventStartDate'
                ) THEN
                    UPDATE "Events"
                    SET "PreferredDate" = "EventStartDate"
                    WHERE "PreferredDate" IS NULL;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'EventEndDate'
                ) THEN
                    UPDATE "Events"
                    SET "EventDuration" = "EventEndDate" - "EventStartDate"
                    WHERE "EventDuration" IS NULL
                      AND "EventEndDate" > "EventStartDate";
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'PreferredLocation'
                ) THEN
                    UPDATE "Events"
                    SET "PreferredVenue" = "PreferredLocation"
                    WHERE "PreferredVenue" IS NULL;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'RequirementNotes'
                ) THEN
                    UPDATE "Events"
                    SET "Requirements" = "RequirementNotes"
                    WHERE "Requirements" IS NULL;
                END IF;
            END
            $migration$;

            UPDATE "Events"
            SET "CreatedAt" = COALESCE("CreatedAt", now()),
                "EventType" = COALESCE("EventType", 0),
                "EventName" = COALESCE(NULLIF(btrim("EventName"), ''), "EventType"::text),
                "GuestCount" = COALESCE("GuestCount", 1),
                "Budget" = COALESCE("Budget", 0),
                "PreferredDate" = COALESCE("PreferredDate", "CreatedAt", now()),
                "EventDuration" = COALESCE(NULLIF("EventDuration", interval '0'), interval '1 hour'),
                "Status" = COALESCE("Status", 0);

            DO $migration$
            BEGIN
                IF EXISTS (SELECT 1 FROM "Events" WHERE "OwnerId" IS NULL) THEN
                    RAISE EXCEPTION 'Cannot reconcile Events: one or more rows have no mappable owner.';
                END IF;

                ALTER TABLE "Events"
                    ALTER COLUMN "OwnerId" SET NOT NULL,
                    ALTER COLUMN "EventName" SET NOT NULL,
                    ALTER COLUMN "EventType" SET NOT NULL,
                    ALTER COLUMN "GuestCount" SET NOT NULL,
                    ALTER COLUMN "Budget" SET NOT NULL,
                    ALTER COLUMN "PreferredDate" SET NOT NULL,
                    ALTER COLUMN "EventDuration" SET NOT NULL,
                    ALTER COLUMN "Status" SET DEFAULT 0,
                    ALTER COLUMN "Status" SET NOT NULL,
                    ALTER COLUMN "CreatedAt" SET NOT NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'CK_Events_Budget_NonNegative'
                      AND conrelid = '"Events"'::regclass
                ) THEN
                    ALTER TABLE "Events"
                        ADD CONSTRAINT "CK_Events_Budget_NonNegative" CHECK ("Budget" >= 0);
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'CK_Events_GuestCount_Positive'
                      AND conrelid = '"Events"'::regclass
                ) THEN
                    ALTER TABLE "Events"
                        ADD CONSTRAINT "CK_Events_GuestCount_Positive" CHECK ("GuestCount" > 0);
                END IF;
            END
            $migration$;

            DO $migration$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'CreatedByUserId'
                ) THEN
                    ALTER TABLE "Events" ALTER COLUMN "CreatedByUserId" DROP NOT NULL;
                END IF;
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'EventStartDate'
                ) THEN
                    ALTER TABLE "Events" ALTER COLUMN "EventStartDate" DROP NOT NULL;
                END IF;
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Events'
                      AND column_name = 'EventEndDate'
                ) THEN
                    ALTER TABLE "Events" ALTER COLUMN "EventEndDate" DROP NOT NULL;
                END IF;
            END
            $migration$;

            CREATE INDEX IF NOT EXISTS "IX_Events_OwnerId" ON "Events" ("OwnerId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Event schema reconciliation is forward-only to prevent loss of guest event data.");
    }
}
