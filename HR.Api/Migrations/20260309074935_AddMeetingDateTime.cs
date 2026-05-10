using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "MeetingDate",
                table: "Meetings",
                type: "date",
                nullable: false,
                defaultValueSql: "CAST(GETUTCDATE() AS date)");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MeetingTime",
                table: "Meetings",
                type: "time",
                nullable: false,
                defaultValueSql: "CAST(GETUTCDATE() AS time)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingDate",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingTime",
                table: "Meetings");
        }
    }
}
