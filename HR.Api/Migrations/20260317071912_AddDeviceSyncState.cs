using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceSyncStates",
                columns: table => new
                {
                    DeviceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSyncedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSyncStates", x => x.DeviceKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceSyncStates");
        }
    }
}
