using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentReportingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordHashing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure PasswordHash and PasswordSalt are present as bytea.
            // If columns already exist with a different type (e.g., text), convert using explicit USING clause.
            migrationBuilder.Sql("""
                                 DO $$
                                 BEGIN
                                     -- PasswordHash
                                     IF NOT EXISTS (
                                         SELECT 1 FROM information_schema.columns
                                         WHERE table_name = 'Users' AND column_name = 'PasswordHash'
                                     ) THEN
                                         ALTER TABLE "Users" ADD COLUMN "PasswordHash" bytea NOT NULL DEFAULT E'\\x';
                                     ELSE
                                         ALTER TABLE "Users" ALTER COLUMN "PasswordHash" TYPE bytea USING "PasswordHash"::bytea;
                                         ALTER TABLE "Users" ALTER COLUMN "PasswordHash" SET NOT NULL;
                                     END IF;
                                 
                                     -- PasswordSalt
                                     IF NOT EXISTS (
                                         SELECT 1 FROM information_schema.columns
                                         WHERE table_name = 'Users' AND column_name = 'PasswordSalt'
                                     ) THEN
                                         ALTER TABLE "Users" ADD COLUMN "PasswordSalt" bytea NOT NULL DEFAULT E'\\x';
                                     ELSE
                                         ALTER TABLE "Users" ALTER COLUMN "PasswordSalt" TYPE bytea USING "PasswordSalt"::bytea;
                                         ALTER TABLE "Users" ALTER COLUMN "PasswordSalt" SET NOT NULL;
                                     END IF;
                                 END $$;
                                 """);

            migrationBuilder.Sql("""ALTER TABLE "Users" ALTER COLUMN "PasswordHash" DROP DEFAULT;""");
            migrationBuilder.Sql("""ALTER TABLE "Users" ALTER COLUMN "PasswordSalt" DROP DEFAULT;""");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordSalt",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");
        }
    }
}
