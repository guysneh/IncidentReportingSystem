using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_attachments_ParentType_ParentId_ContentType",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "ContentType" });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ParentType_ParentId_CreatedAt",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ParentType_ParentId_FileName",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ParentType_ParentId_Size",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "Size" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attachments_ParentType_ParentId_ContentType",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_ParentType_ParentId_CreatedAt",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_ParentType_ParentId_FileName",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_ParentType_ParentId_Size",
                table: "attachments");
        }
    }
}
