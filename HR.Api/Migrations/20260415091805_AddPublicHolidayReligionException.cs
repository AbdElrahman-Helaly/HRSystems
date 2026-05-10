using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicHolidayReligionException : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Religion",
                table: "PublicHolidayExceptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayExceptions_Religion",
                table: "PublicHolidayExceptions",
                column: "Religion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicHolidayExceptions_Religion",
                table: "PublicHolidayExceptions");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "PublicHolidayExceptions");
        }

    }
}
