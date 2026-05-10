using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class ثث : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeWeeklyShifts");

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceAllowance",
                table: "Users",
                type: "decimal(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceAllowance",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "EmployeeWeeklyShifts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWeeklyShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWeeklyShifts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWeeklyShifts_UserId",
                table: "EmployeeWeeklyShifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWeeklyShifts_UserId_DayOfWeek",
                table: "EmployeeWeeklyShifts",
                columns: new[] { "UserId", "DayOfWeek" });
        }
    }
}
