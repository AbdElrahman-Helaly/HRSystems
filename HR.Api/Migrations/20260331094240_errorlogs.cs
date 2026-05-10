using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class errorlogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Path = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    QueryString = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RemoteIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorLogs_CreatedAt",
                table: "SystemErrorLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorLogs_UserId",
                table: "SystemErrorLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemErrorLogs");
        }
    }
}
