using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class addsalary : Migration
    {
       
        /// <inheritdoc />
            /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable(
                    name: "SalaryAdvances",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                        MonthlyDeduction = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                        NumberOfMonths = table.Column<int>(type: "int", nullable: false),
                        StartDate = table.Column<DateTime>(type: "date", nullable: false),
                        Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                        Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                        RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                        CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                        CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                        ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_SalaryAdvances", x => x.Id);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_SalaryAdvances_UserId",
                    table: "SalaryAdvances",
                    column: "UserId");
            }

            /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(
                    name: "SalaryAdvances");
            }
        }
    }

    /// <inheritdoc />

