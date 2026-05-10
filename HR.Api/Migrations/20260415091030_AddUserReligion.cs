using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReligion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Religion",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Religion",
                table: "Users");
        }
    }
}
