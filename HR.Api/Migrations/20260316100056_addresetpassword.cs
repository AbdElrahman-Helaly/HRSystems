using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class addresetpassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OtpSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_PhoneNumber",
                table: "PasswordResetOtps",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_PhoneNumber_IsUsed_ExpiresAt",
                table: "PasswordResetOtps",
                columns: new[] { "PhoneNumber", "IsUsed", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetOtps");
        }
    }
}
