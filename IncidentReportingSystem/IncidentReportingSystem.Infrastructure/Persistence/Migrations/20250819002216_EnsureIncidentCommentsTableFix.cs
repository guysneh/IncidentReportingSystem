using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureIncidentCommentsTableFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM   information_schema.tables
        WHERE  table_schema = 'public' AND table_name = 'incident_comments'
    ) THEN
        CREATE TABLE public.incident_comments (
            ""Id"" uuid NOT NULL,
            ""IncidentId"" uuid NOT NULL,
            ""UserId"" uuid NOT NULL,
            ""Text"" text NOT NULL,
            ""CreatedAtUtc"" timestamp without time zone NOT NULL,
            CONSTRAINT pk_incident_comments PRIMARY KEY (""Id"")
        );

        CREATE INDEX IF NOT EXISTS ix_incident_comments_incident_id
            ON public.incident_comments (""IncidentId"");

        CREATE INDEX IF NOT EXISTS ix_incident_comments_incident_id_createdatutc
            ON public.incident_comments (""IncidentId"", ""CreatedAtUtc"");

        ALTER TABLE public.incident_comments
            ADD CONSTRAINT fk_incident_comments_incident_reports_incidentid
            FOREIGN KEY (""IncidentId"")
            REFERENCES public.""IncidentReports"" (""Id"")
            ON DELETE CASCADE;
    END IF;
END
$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM   information_schema.tables
        WHERE  table_schema = 'public' AND table_name = 'incident_comments'
    ) THEN
        DROP TABLE public.incident_comments;
    END IF;
END
$$;
");
        }
    }
}
