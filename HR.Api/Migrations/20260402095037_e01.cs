using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class e01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShiftRate",
                table: "Users",
                type: "decimal(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftRate",
                table: "Users");
        }
    }
}
