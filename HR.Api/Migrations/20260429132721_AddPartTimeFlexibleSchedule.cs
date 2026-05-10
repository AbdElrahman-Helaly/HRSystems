using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddPartTimeFlexibleSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartTimeCustomDaysJson",
                table: "EmployeeWorkSchedules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartTimeUseDefaultWeek",
                table: "EmployeeWorkSchedules",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartTimeCustomDaysJson",
                table: "EmployeeWorkSchedules");

            migrationBuilder.DropColumn(
                name: "PartTimeUseDefaultWeek",
                table: "EmployeeWorkSchedules");
        }
    }
}
