using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentType = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasThumbnail = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_CreatedAt",
                table: "attachments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ParentType_ParentId",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");
        }
    }
}
