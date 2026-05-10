using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecruitmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    RequestedJobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiredCount = table.Column<int>(type: "int", nullable: false),
                    RequiredExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Skills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HrResponseNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentRequests_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecruitmentRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecruitmentCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruitmentRequestId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CvFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CvOriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CvFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CvContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CvFileSize = table.Column<long>(type: "bigint", nullable: false),
                    SubmittedByHrUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ManagerResponseNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentCandidates_RecruitmentRequests_RecruitmentRequestId",
                        column: x => x.RecruitmentRequestId,
                        principalTable: "RecruitmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecruitmentCandidates_Users_SubmittedByHrUserId",
                        column: x => x.SubmittedByHrUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCandidates_RecruitmentRequestId",
                table: "RecruitmentCandidates",
                column: "RecruitmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCandidates_Status",
                table: "RecruitmentCandidates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCandidates_SubmittedByHrUserId",
                table: "RecruitmentCandidates",
                column: "SubmittedByHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentRequests_DepartmentId",
                table: "RecruitmentRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentRequests_RequestedByUserId",
                table: "RecruitmentRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentRequests_Status",
                table: "RecruitmentRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecruitmentCandidates");

            migrationBuilder.DropTable(
                name: "RecruitmentRequests");
        }
    }
}
