using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "users");
        }
    }
}
