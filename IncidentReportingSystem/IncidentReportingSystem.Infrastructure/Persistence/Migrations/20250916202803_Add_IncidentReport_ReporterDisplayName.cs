using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_IncidentReport_ReporterDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incident_comments_IncidentReports_IncidentId",
                schema: "public",
                table: "incident_comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncidentReports",
                table: "IncidentReports");

            migrationBuilder.RenameTable(
                name: "IncidentReports",
                newName: "incident_reports");

            migrationBuilder.AlterColumn<string>(
                name: "SystemAffected",
                table: "incident_reports",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "incident_reports",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "incident_reports",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "incident_reports",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "incident_reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ReporterDisplayName",
                table: "incident_reports",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_incident_reports",
                table: "incident_reports",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_CreatedAt",
                table: "incident_reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_ReporterId",
                table: "incident_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_Status_Severity",
                table: "incident_reports",
                columns: new[] { "Status", "Severity" });

            migrationBuilder.AddForeignKey(
                name: "FK_incident_comments_incident_reports_IncidentId",
                schema: "public",
                table: "incident_comments",
                column: "IncidentId",
                principalTable: "incident_reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incident_comments_incident_reports_IncidentId",
                schema: "public",
                table: "incident_comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_incident_reports",
                table: "incident_reports");

            migrationBuilder.DropIndex(
                name: "IX_incident_reports_CreatedAt",
                table: "incident_reports");

            migrationBuilder.DropIndex(
                name: "IX_incident_reports_ReporterId",
                table: "incident_reports");

            migrationBuilder.DropIndex(
                name: "IX_incident_reports_Status_Severity",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "ReporterDisplayName",
                table: "incident_reports");

            migrationBuilder.RenameTable(
                name: "incident_reports",
                newName: "IncidentReports");

            migrationBuilder.AlterColumn<string>(
                name: "SystemAffected",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "IncidentReports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Severity",
                table: "IncidentReports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncidentReports",
                table: "IncidentReports",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_incident_comments_IncidentReports_IncidentId",
                schema: "public",
                table: "incident_comments",
                column: "IncidentId",
                principalTable: "IncidentReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
