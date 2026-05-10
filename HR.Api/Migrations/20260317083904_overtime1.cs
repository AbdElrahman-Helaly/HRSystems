using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class overtime1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Overtimes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Overtimes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalHours",
                table: "Overtimes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Overtimes");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Overtimes");

            migrationBuilder.DropColumn(
                name: "TotalHours",
                table: "Overtimes");
        }
    }
}
