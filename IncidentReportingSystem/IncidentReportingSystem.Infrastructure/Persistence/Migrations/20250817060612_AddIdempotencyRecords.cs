using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedEmail",
                table: "users");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    ResponseContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresUtc",
                table: "IdempotencyRecords",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedEmail",
                table: "users",
                column: "NormalizedEmail",
                unique: true);
        }
    }
}
