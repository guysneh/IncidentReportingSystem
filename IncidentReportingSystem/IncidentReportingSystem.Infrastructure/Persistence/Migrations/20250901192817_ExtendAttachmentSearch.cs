using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAttachmentSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Parent_CreatedAt",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Parent_FileName",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Parent_Size",
                table: "attachments",
                columns: new[] { "ParentType", "ParentId", "Size" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attachments_Parent_CreatedAt",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_Parent_FileName",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_Parent_Size",
                table: "attachments");
        }
    }
}
