using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Ensures 'public.incident_comments' exists with required indexes and FK to "IncidentReports".
    /// Safe to run multiple times (IF NOT EXISTS and duplicate_object handling).
    /// </summary>
    public partial class EnsureIncidentCommentsTableFinal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- Create table if missing
    IF to_regclass('public.incident_comments') IS NULL THEN
        CREATE TABLE public.incident_comments
        (
            ""Id"" uuid NOT NULL,
            ""IncidentId"" uuid NOT NULL,
            ""UserId"" uuid NOT NULL,
            ""Text"" character varying(2000) NOT NULL,
            ""CreatedAtUtc"" timestamp with time zone NOT NULL,
            CONSTRAINT ""PK_incident_comments"" PRIMARY KEY (""Id"")
        );
    END IF;

    -- Indexes (ignore if already exist)
    BEGIN
        CREATE INDEX ""IX_incident_comments_IncidentId"" ON public.incident_comments (""IncidentId"");
    EXCEPTION WHEN duplicate_table THEN
        -- ignore
    END;

    BEGIN
        CREATE INDEX ""IX_incident_comments_IncidentId_CreatedAtUtc""
            ON public.incident_comments (""IncidentId"", ""CreatedAtUtc"");
    EXCEPTION WHEN duplicate_table THEN
        -- ignore
    END;

    -- Foreign key to ""IncidentReports"" on IncidentId (ignore if already exists)
    BEGIN
        ALTER TABLE public.incident_comments
            ADD CONSTRAINT ""FK_incident_comments_IncidentReports_IncidentId""
            FOREIGN KEY (""IncidentId"") REFERENCES public.""IncidentReports"" (""Id"")
            ON DELETE CASCADE;
    EXCEPTION WHEN duplicate_object THEN
        -- ignore
    END;
END
$$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: do not drop table on down to avoid accidental data loss.
            // If you must support down, you could drop the FK and table here.
        }
    }
}
