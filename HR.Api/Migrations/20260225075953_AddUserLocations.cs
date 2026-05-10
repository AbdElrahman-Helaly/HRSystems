using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_LocationId",
                table: "Attendances",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_UserLocations_LocationId",
                table: "Attendances",
                column: "LocationId",
                principalTable: "UserLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_UserLocations_LocationId",
                table: "Attendances");

            migrationBuilder.DropTable(
                name: "UserLocations");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_LocationId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Attendances");
        }
    }
}
