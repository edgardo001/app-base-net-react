using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBaseNetReact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailAndNormalizedEmailPartialUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing full unique indexes; recreate them as
            // partial unique indexes constrained to non-soft-deleted rows.
            // This fixes the production bug where a user with email X is
            // soft-deleted (DeletedAt != null) and a new user with email X
            // cannot be created because the pre-check GetByEmailAsync
            // (query-filter aware, returns null) is bypassed and PostgreSQL
            // rejects the INSERT with 23505 IX_Users_Email.
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);
        }
    }
}
