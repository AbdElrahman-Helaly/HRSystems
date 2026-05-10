using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace internalEmployee.Migrations
{
    /// <inheritdoc />
    public partial class iniatial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Where = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DepartureTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MachineCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LateDeductionHours = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    LateDeductionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FcmTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcmTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governorates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CompanyPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobLevel = table.Column<int>(type: "int", nullable: true),
                    IsManagerRole = table.Column<bool>(type: "bit", nullable: false),
                    ParentJobId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitles_JobTitles_ParentJobId",
                        column: x => x.ParentJobId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Leaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LeaveType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MedicalReportUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaritalStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaritalStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nationalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxBrackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ToAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GovernorateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsMale = table.Column<bool>(type: "bit", nullable: false),
                    IsPending = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirstNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MiddleNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MiddleNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MachineCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FingerprintKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    JobId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    AddressAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmploymentModeId = table.Column<int>(type: "int", nullable: true),
                    GovernorateId = table.Column<int>(type: "int", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    HousingAllowance = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    MealAllowance = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TransportationAllowance = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    OvertimeRate = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    InsuranceSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    IsInsured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: true),
                    CompanyPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SickLeaveBalance = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    WorkType = table.Column<int>(type: "int", nullable: false),
                    WorkFromHomeDays = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_NationalId_Or_PassportNumber", "([NationalId] IS NOT NULL) OR ([PassportNumber] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_EmploymentModes_EmploymentModeId",
                        column: x => x.EmploymentModeId,
                        principalTable: "EmploymentModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_JobTitles_JobId",
                        column: x => x.JobId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBankInfos",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SwiftBic = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBankInfos", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_EmployeeBankInfos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    BonusDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBonuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEducations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GraduationYear = table.Column<DateOnly>(type: "date", nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FinalGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DoneBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OldSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    NewSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    OldDepartmentId = table.Column<int>(type: "int", nullable: true),
                    NewDepartmentId = table.Column<int>(type: "int", nullable: true),
                    OldBranchId = table.Column<int>(type: "int", nullable: true),
                    NewBranchId = table.Column<int>(type: "int", nullable: true),
                    OldJobId = table.Column<int>(type: "int", nullable: true),
                    NewJobId = table.Column<int>(type: "int", nullable: true),
                    OldManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeHistories_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeHistories_Users_DoneBy",
                        column: x => x.DoneBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeHistories_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PenaltyType = table.Column<int>(type: "int", nullable: false),
                    Days = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    PenaltyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AppliedMonth = table.Column<int>(type: "int", nullable: true),
                    AppliedYear = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePenalties_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWeeklyShifts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "EmployeeWorkSchedules",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartTimeStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    PartTimeEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    FullTimeStartOverride = table.Column<TimeSpan>(type: "time", nullable: true),
                    FullTimeEndOverride = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkSchedules", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidayExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicHolidayId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    EmploymentModeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidayExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicHolidayExceptions_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicHolidayExceptions_EmploymentModes_EmploymentModeId",
                        column: x => x.EmploymentModeId,
                        principalTable: "EmploymentModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicHolidayExceptions_PublicHolidays_PublicHolidayId",
                        column: x => x.PublicHolidayId,
                        principalTable: "PublicHolidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicHolidayExceptions_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_UserId",
                table: "Assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Date",
                table: "Attendances",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UserId",
                table: "Attendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UserId_Date",
                table: "Attendances",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_GovernorateId",
                table: "Cities",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBonuses_UserId",
                table: "EmployeeBonuses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_UserId",
                table: "EmployeeEducations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_ApprovedBy",
                table: "EmployeeHistories",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_DoneBy",
                table: "EmployeeHistories",
                column: "DoneBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EmployeeId",
                table: "EmployeeHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EmployeeId_EventType",
                table: "EmployeeHistories",
                columns: new[] { "EmployeeId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EventDate",
                table: "EmployeeHistories",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EventType",
                table: "EmployeeHistories",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePenalties_IsApplied",
                table: "EmployeePenalties",
                column: "IsApplied");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePenalties_UserId",
                table: "EmployeePenalties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWeeklyShifts_UserId",
                table: "EmployeeWeeklyShifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWeeklyShifts_UserId_DayOfWeek",
                table: "EmployeeWeeklyShifts",
                columns: new[] { "UserId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkSchedules_UserId",
                table: "EmployeeWorkSchedules",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcmTokens_Token",
                table: "FcmTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcmTokens_UserId",
                table: "FcmTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_ParentJobId",
                table: "JobTitles",
                column: "ParentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_UserId",
                table: "Leaves",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_RequestId",
                table: "Notifications",
                columns: new[] { "Type", "RequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId",
                table: "Permissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayExceptions_DepartmentId",
                table: "PublicHolidayExceptions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayExceptions_EmployeeId",
                table: "PublicHolidayExceptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayExceptions_EmploymentModeId",
                table: "PublicHolidayExceptions",
                column: "EmploymentModeId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayExceptions_PublicHolidayId",
                table: "PublicHolidayExceptions",
                column: "PublicHolidayId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Year",
                table: "PublicHolidays",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_TaxBrackets_Order",
                table: "TaxBrackets",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttachments_UserId",
                table: "UserAttachments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CityId",
                table: "Users",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmploymentModeId",
                table: "Users",
                column: "EmploymentModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GovernorateId",
                table: "Users",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_InsuranceCompanyId",
                table: "Users",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_JobId",
                table: "Users",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MaritalStatusId",
                table: "Users",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NationalId",
                table: "Users",
                column: "NationalId",
                unique: true,
                filter: "[NationalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NationalityId",
                table: "Users",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PassportNumber",
                table: "Users",
                column: "PassportNumber",
                unique: true,
                filter: "[PassportNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "EmployeeBankInfos");

            migrationBuilder.DropTable(
                name: "EmployeeBonuses");

            migrationBuilder.DropTable(
                name: "EmployeeEducations");

            migrationBuilder.DropTable(
                name: "EmployeeHistories");

            migrationBuilder.DropTable(
                name: "EmployeePenalties");

            migrationBuilder.DropTable(
                name: "EmployeeWeeklyShifts");

            migrationBuilder.DropTable(
                name: "EmployeeWorkSchedules");

            migrationBuilder.DropTable(
                name: "FcmTokens");

            migrationBuilder.DropTable(
                name: "InsuranceSettings");

            migrationBuilder.DropTable(
                name: "Leaves");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PublicHolidayExceptions");

            migrationBuilder.DropTable(
                name: "TaxBrackets");

            migrationBuilder.DropTable(
                name: "UserAttachments");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "EmploymentModes");

            migrationBuilder.DropTable(
                name: "InsuranceCompanies");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "MaritalStatuses");

            migrationBuilder.DropTable(
                name: "Nationalities");

            migrationBuilder.DropTable(
                name: "Governorates");
        }
    }
}
