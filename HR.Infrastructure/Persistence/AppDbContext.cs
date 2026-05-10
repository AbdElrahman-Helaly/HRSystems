using internalEmployee.Auth.Models;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace internalEmployee.Data;

public sealed class AppDbContext : DbContext
{
    private Guid? _currentUserId;
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Set the current user ID for tracking who made changes
    public void SetCurrentUser(Guid? userId)
    {
        _currentUserId = userId;
    }

    // Override SaveChangesAsync to automatically track employee changes
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await TrackEmployeeChangesAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task TrackEmployeeChangesAsync(CancellationToken cancellationToken)
    {
        var entries = ChangeTracker.Entries<AppUser>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        if (!entries.Any()) return;

        // Verify that the performer user still exists in DB to prevent FK conflict
        // (common when DB is reset/re-seeded but browser still has an old JWT token)
        if (_currentUserId.HasValue)
        {
            var userExists = await Users.AnyAsync(u => u.Id == _currentUserId.Value, cancellationToken);
            if (!userExists)
            {
                _currentUserId = null; // Stale token or deleted user, set to null to avoid FK error
            }
        }

        foreach (var entry in entries)
        {
            var employeeId = entry.Entity.Id;
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            // 1. Salary Change
            if (entry.Property(e => e.GrossSalary).IsModified)
            {
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.SalaryChange,
                    OldSalary = entry.Property(e => e.GrossSalary).OriginalValue,
                    NewSalary = entry.Property(e => e.GrossSalary).CurrentValue,
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي للراتب"
                });
            }

            // 2. Job Title Change
            if (entry.Property(e => e.JobId).IsModified)
            {
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.JobTitleChange,
                    OldJobId = entry.Property(e => e.JobId).OriginalValue,
                    NewJobId = entry.Property(e => e.JobId).CurrentValue,
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي للمسمى الوظيفي"
                });
            }

            // 3. Department Change
            if (entry.Property(e => e.DepartmentId).IsModified)
            {
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.DepartmentChange,
                    OldDepartmentId = entry.Property(e => e.DepartmentId).OriginalValue,
                    NewDepartmentId = entry.Property(e => e.DepartmentId).CurrentValue,
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي للقسم"
                });
            }

            // 4. Branch Change
            if (entry.Property(e => e.BranchId).IsModified)
            {
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.BranchChange,
                    OldBranchId = entry.Property(e => e.BranchId).OriginalValue,
                    NewBranchId = entry.Property(e => e.BranchId).CurrentValue,
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي للفرع"
                });
            }

            // 5. Manager Change
            if (entry.Property(e => e.ManagerId).IsModified)
            {
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.ManagerChange,
                    OldManagerId = entry.Property(e => e.ManagerId).OriginalValue,
                    NewManagerId = entry.Property(e => e.ManagerId).CurrentValue,
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي للمدير المباشر"
                });
            }

            // 6. Contract End Date (Contract Renewed)
            if (entry.Property(e => e.ContractEndDate).IsModified)
            {
                var oldDate = entry.Property(e => e.ContractEndDate).OriginalValue;
                var newDate = entry.Property(e => e.ContractEndDate).CurrentValue;
                
                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.ContractRenewed,
                    OldValue = oldDate?.ToString("yyyy-MM-dd"),
                    NewValue = newDate?.ToString("yyyy-MM-dd"),
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث تلقائي لتاريخ انتهاء العقد"
                });
            }

            // 7. Active Status (Termination/Hiring)
            if (entry.Property(e => e.IsActive).IsModified)
            {
                var wasActive = entry.Property(e => e.IsActive).OriginalValue;
                var isActive = entry.Property(e => e.IsActive).CurrentValue;

                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = isActive ? EmployeeEventType.Hired : EmployeeEventType.Terminated,
                    OldValue = wasActive ? "نشط" : "غير نشط",
                    NewValue = isActive ? "نشط" : "غير نشط",
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = isActive ? "إعادة تفعيل الموظف" : "إنهاء الخدمة"
                });
            }

            // 8. Allowance and Insurance Salary Changes
            if (entry.Property(e => e.HousingAllowance).IsModified || 
                entry.Property(e => e.MealAllowance).IsModified || 
                entry.Property(e => e.TransportationAllowance).IsModified ||
                entry.Property(e => e.InsuranceAllowance).IsModified ||
                entry.Property(e => e.InsuranceSalary).IsModified)
            {
                var oldH = entry.Property(e => e.HousingAllowance).OriginalValue;
                var newH = entry.Property(e => e.HousingAllowance).CurrentValue;
                var oldM = entry.Property(e => e.MealAllowance).OriginalValue;
                var newM = entry.Property(e => e.MealAllowance).CurrentValue;
                var oldT = entry.Property(e => e.TransportationAllowance).OriginalValue;
                var newT = entry.Property(e => e.TransportationAllowance).CurrentValue;
                var oldIA = entry.Property(e => e.InsuranceAllowance).OriginalValue;
                var newIA = entry.Property(e => e.InsuranceAllowance).CurrentValue;
                var oldIns = entry.Property(e => e.InsuranceSalary).OriginalValue;
                var newIns = entry.Property(e => e.InsuranceSalary).CurrentValue;

                EmployeeHistories.Add(new EmployeeHistory
                {
                    EmployeeId = employeeId,
                    EventType = EmployeeEventType.AllowanceChange,
                    OldValue = $"Housing: {oldH}, Meal: {oldM}, Transport: {oldT}, InsAllowance: {oldIA}, InsSalary: {oldIns}",
                    NewValue = $"Housing: {newH}, Meal: {newM}, Transport: {newT}, InsAllowance: {newIA}, InsSalary: {newIns}",
                    EventDate = now,
                    EffectiveDate = today,
                    DoneBy = _currentUserId,
                    Reason = "تحديث البدلات أو الراتب التأميني"
                });
            }
        }

        await Task.CompletedTask;
    }



    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<MaritalStatus> MaritalStatuses => Set<MaritalStatus>();
    public DbSet<EmploymentMode> EmploymentModes => Set<EmploymentMode>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Overtime> Overtimes => Set<Overtime>();
    public DbSet<SalaryAdvance> SalaryAdvances => Set<SalaryAdvance>();
    public DbSet<HealthInsuranceEnrollment> HealthInsuranceEnrollments => Set<HealthInsuranceEnrollment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FcmToken> FcmTokens => Set<FcmToken>();
    public DbSet<UserAttachment> UserAttachments => Set<UserAttachment>();
    public DbSet<EmployeeBankInfo> EmployeeBankInfos => Set<EmployeeBankInfo>();
    public DbSet<EmployeeEducation> EmployeeEducations => Set<EmployeeEducation>();
    public DbSet<EmployeeWorkSchedule> EmployeeWorkSchedules => Set<EmployeeWorkSchedule>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<PublicHolidayException> PublicHolidayExceptions => Set<PublicHolidayException>();
    public DbSet<InsuranceSettings> InsuranceSettings => Set<InsuranceSettings>();
    public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();
    public DbSet<EmployeePenalty> EmployeePenalties => Set<EmployeePenalty>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
    public DbSet<EmployeeBonus> EmployeeBonuses => Set<EmployeeBonus>();
    public DbSet<EmployeeHistory> EmployeeHistories => Set<EmployeeHistory>();
    public DbSet<SystemErrorLog> SystemErrorLogs => Set<SystemErrorLog>();
        public DbSet<Meeting> Meetings => Set<Meeting>();
        public DbSet<MeetingDepartment> MeetingDepartments => Set<MeetingDepartment>();
        public DbSet<MeetingAttachment> MeetingAttachments => Set<MeetingAttachment>();
    public DbSet<DeviceSyncState> DeviceSyncStates => Set<DeviceSyncState>();
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<CustodyItem> CustodyItems => Set<CustodyItem>();
    public DbSet<EmployeeCustody> EmployeeCustodies => Set<EmployeeCustody>();
    public DbSet<RecruitmentRequest> RecruitmentRequests => Set<RecruitmentRequest>();
    public DbSet<RecruitmentCandidate> RecruitmentCandidates => Set<RecruitmentCandidate>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<WorkFromHomeRequest> WorkFromHomeRequests => Set<WorkFromHomeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(b =>
        {
            b.ToTable("Department");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Nationality>(b =>
        {
            b.ToTable("Nationalities");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(100);
        });

        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("Branches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(200);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<Governorate>(b =>
        {
            b.ToTable("Governorates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(100);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<City>(b =>
        {
            b.ToTable("Cities");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(100);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.HasOne<Governorate>()
                .WithMany()
                .HasForeignKey(x => x.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.GovernorateId);
        });

        modelBuilder.Entity<JobTitle>(b =>
        {
            b.ToTable("JobTitles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(200);
            b.HasOne<JobTitle>()
                .WithMany()
                .HasForeignKey(x => x.ParentJobId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MaritalStatus>(b =>
        {
            b.ToTable("MaritalStatuses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(50);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<EmploymentMode>(b =>
        {
            b.ToTable("EmploymentModes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(50);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            
            b.Property(x => x.NationalId)
                .HasMaxLength(50);
            
            b.HasIndex(x => x.NationalId)
                .IsUnique()
                .HasFilter("[NationalId] IS NOT NULL");

            b.Property(x => x.PassportNumber)
                .HasMaxLength(50);

            b.HasIndex(x => x.PassportNumber)
                .IsUnique()
                .HasFilter("[PassportNumber] IS NOT NULL");

            // Check constraint: Either NationalId or PassportNumber must be provided
            b.ToTable(t => t.HasCheckConstraint("CK_Users_NationalId_Or_PassportNumber", 
                "([NationalId] IS NOT NULL) OR ([PassportNumber] IS NOT NULL)"));

            b.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            b.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            b.Property(x => x.IsPending)
                .IsRequired()
                .HasDefaultValue(false);

            b.Property(x => x.Email)
                .HasMaxLength(200);

            b.Property(x => x.Religion);

            // Arabic name fields
            b.Property(x => x.FirstNameAr)
                .HasMaxLength(100);
            b.Property(x => x.MiddleNameAr)
                .HasMaxLength(100);
            b.Property(x => x.LastNameAr)
                .HasMaxLength(100);

            // English name fields
            b.Property(x => x.FirstNameEn)
                .HasMaxLength(100);
            b.Property(x => x.MiddleNameEn)
                .HasMaxLength(100);
            b.Property(x => x.LastNameEn)
                .HasMaxLength(100);

            b.Property(x => x.MachineCode)
                .HasMaxLength(100);

            b.Property(x => x.FingerprintKey)
                .HasMaxLength(500);

            b.Property(x => x.AllowMobileAttendanceFromAnyLocation)
                .IsRequired()
                .HasDefaultValue(false);

            b.Property(x => x.EmployeeCode)
                .HasMaxLength(50);

            b.HasIndex(x => x.EmployeeCode)
                .IsUnique()
                .HasFilter("[EmployeeCode] IS NOT NULL");

            b.Property(x => x.AddressAr)
                .HasMaxLength(500);

            b.Property(x => x.AddressEn)
                .HasMaxLength(500);

            b.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            b.Property(x => x.IsDisabled)
                .IsRequired()
                .HasDefaultValue(false);

            b.Property(x => x.SickLeaveBalance)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            b.HasIndex(x => x.PhoneNumber)
                .IsUnique();

            b.Property(x => x.JobTitle)
                .HasMaxLength(200);

            b.Property(x => x.CompanyPhoneNumber)
                .HasMaxLength(20);

            b.Property(x => x.CompanyEmail)
                .HasMaxLength(200);

            b.Property(x => x.GrossSalary)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.ShiftRate)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.HousingAllowance)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.MealAllowance)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.TransportationAllowance)
                .HasColumnType("decimal(12,2)");
            
            b.Property(x => x.InsuranceAllowance)
                .HasColumnType("decimal(12,2)");

            // DateOnly is stored as SQL 'date' by EF8
            b.Property(x => x.ContractEndDate)
                .HasColumnType("date");

            b.Property(x => x.OvertimeRate)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.InsuranceSalary)
                .HasColumnType("decimal(12,2)");

            b.Property(x => x.IsInsured)
                .HasDefaultValue(false);

            // Foreign key to Nationality
            b.HasOne<Nationality>()
                .WithMany()
                .HasForeignKey(x => x.NationalityId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Branch
            b.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to JobTitle
            b.HasOne<JobTitle>()
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Manager (self-referencing)
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Foreign key to MaritalStatus
            b.HasOne<MaritalStatus>()
                .WithMany()
                .HasForeignKey(x => x.MaritalStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to EmploymentMode
            b.HasOne<EmploymentMode>()
                .WithMany()
                .HasForeignKey(x => x.EmploymentModeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to Governorate
            b.HasOne<Governorate>()
                .WithMany()
                .HasForeignKey(x => x.GovernorateId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to City
            b.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key to InsuranceCompany
            b.HasOne<InsuranceCompany>()
                .WithMany()
                .HasForeignKey(x => x.InsuranceCompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            // BankInfo (1:1)
            b.HasOne(x => x.BankInfo)
                .WithOne(x => x.User)
                .HasForeignKey<EmployeeBankInfo>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Educations (1:N)
            b.HasMany(x => x.Educations)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkSchedule (1:1)
            b.HasOne(x => x.WorkSchedule)
                .WithOne()
                .HasForeignKey<EmployeeWorkSchedule>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<EmployeeBankInfo>(b =>
        {
            b.ToTable("EmployeeBankInfos");
            b.HasKey(x => x.UserId);
            b.Property(x => x.BankName).HasMaxLength(150);
            b.Property(x => x.AccountNumber).HasMaxLength(100);
            b.Property(x => x.Iban).HasMaxLength(50);
            b.Property(x => x.SwiftBic).HasMaxLength(30);
            b.Property(x => x.BranchCode).HasMaxLength(50);
        });

        modelBuilder.Entity<EmployeeEducation>(b =>
        {
            b.ToTable("EmployeeEducations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.UniversityName).HasMaxLength(200);
            b.Property(x => x.GraduationYear).HasColumnType("date");
            b.Property(x => x.Degree).HasMaxLength(150);
            b.Property(x => x.FinalGrade).HasMaxLength(50);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<EmployeeWorkSchedule>(b =>
        {
            b.ToTable("EmployeeWorkSchedules");
            b.HasKey(x => x.UserId);
            b.Property(x => x.PartTimeStart)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
            b.Property(x => x.PartTimeEnd)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
            b.Property(x => x.PartTimeUseDefaultWeek)
                .IsRequired()
                .HasDefaultValue(true);
            b.Property(x => x.PartTimeCustomDaysJson)
                .HasMaxLength(200);
            b.Property(x => x.FullTimeStartOverride)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
            b.Property(x => x.FullTimeEndOverride)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<UserAttachment>(b =>
        {
            b.ToTable("UserAttachments");
            b.HasKey(x => x.Id);
            b.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            b.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
            b.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<CustodyItem>(b =>
        {
            b.ToTable("CustodyItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<EmployeeCustody>(b =>
        {
            b.ToTable("EmployeeCustodies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CustodyItemId);
            b.HasIndex(x => new { x.UserId, x.CustodyItemId }).IsUnique();
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<CustodyItem>()
                .WithMany()
                .HasForeignKey(x => x.CustodyItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Leave>(b =>
        {
            b.ToTable("Leaves");
            b.HasKey(x => x.Id);
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.LeaveType)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(LeaveType.Annual);
            b.Property(x => x.MedicalReportUrl).HasMaxLength(1000);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Assignment>(b =>
        {
            b.ToTable("Assignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Where).HasMaxLength(200).IsRequired();
            b.Property(x => x.StartDate).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<RecruitmentRequest>(b =>
        {
            b.ToTable("RecruitmentRequests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.RequestedJobTitle).HasMaxLength(200).IsRequired();
            b.Property(x => x.RequiredCount).IsRequired();
            b.Property(x => x.Skills).HasMaxLength(1000).IsRequired(false);
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired(false);
            b.Property(x => x.HrResponseNote).HasMaxLength(1000).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.RequestedByUserId);
            b.HasIndex(x => x.DepartmentId);
            b.HasIndex(x => x.Status);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecruitmentCandidate>(b =>
        {
            b.ToTable("RecruitmentCandidates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired(false);
            b.Property(x => x.Email).HasMaxLength(200).IsRequired(false);
            b.Property(x => x.Notes).HasMaxLength(2000).IsRequired(false);
            b.Property(x => x.CvFileName).HasMaxLength(500).IsRequired();
            b.Property(x => x.CvOriginalFileName).HasMaxLength(500).IsRequired();
            b.Property(x => x.CvFilePath).HasMaxLength(1000).IsRequired();
            b.Property(x => x.CvContentType).HasMaxLength(200).IsRequired();
            b.Property(x => x.ManagerResponseNote).HasMaxLength(1000).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.RecruitmentRequestId);
            b.HasIndex(x => x.SubmittedByHrUserId);
            b.HasIndex(x => x.Status);
            b.HasOne<RecruitmentRequest>()
                .WithMany()
                .HasForeignKey(x => x.RecruitmentRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.SubmittedByHrUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalaryAdvance>(b =>
        {
            b.ToTable("SalaryAdvances");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasColumnType("decimal(12,2)");
            b.Property(x => x.MonthlyDeduction).HasColumnType("decimal(12,2)");
            b.Property(x => x.StartDate).HasColumnType("date");
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<HealthInsuranceEnrollment>(b =>
        {
            b.ToTable("HealthInsuranceEnrollments");
            b.HasKey(x => x.Id);
            b.Property(x => x.MonthlyPremium).HasColumnType("decimal(12,2)");
            b.Property(x => x.StartDate).HasColumnType("date");
            b.Property(x => x.EndDate).HasColumnType("date");
            b.Property(x => x.Notes).HasMaxLength(500);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Attendance>(b =>
        {
            b.ToTable("Attendances");
            b.HasKey(x => x.Id);
            
            // Convert DateOnly to DateTime for storage
            b.Property(x => x.Date)
                .HasConversion(
                    v => v.ToDateTime(TimeOnly.MinValue),
                    v => DateOnly.FromDateTime(v))
                .IsRequired();
            
            // Convert TimeOnly to TimeSpan for storage
            b.Property(x => x.AttendanceTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);
            
            b.Property(x => x.DepartureTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null);

            b.Property(x => x.LateDeductionHours)
                .HasColumnType("decimal(12,2)")
                .IsRequired(false);
            b.Property(x => x.LateDeductionType)
                .HasMaxLength(50)
                .IsRequired(false);
            b.Property(x => x.OvertimeHours)
                .HasColumnType("decimal(12,2)")
                .IsRequired(false);

            // Device information
            b.Property(x => x.DeviceType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired(false);
            
            b.Property(x => x.Location)
                .HasMaxLength(200)
                .IsRequired(false);
            
            b.Property(x => x.MachineCode)
                .HasMaxLength(50)
                .IsRequired(false);

            b.Property(x => x.LocationId)
                .IsRequired(false);

            b.HasOne<UserLocation>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Unique index on UserId + Date to prevent duplicate attendance records per day
            b.HasIndex(x => new { x.UserId, x.Date })
                .IsUnique();
            
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.LocationId);
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable("Notifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Message).HasMaxLength(500).IsRequired();
            b.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.Type, x.RequestId });
                // Meeting entity configuration
                modelBuilder.Entity<Meeting>(m =>
                {
                    m.ToTable("Meetings");
                    m.HasKey(x => x.Id);
                    m.Property(x => x.Title).HasMaxLength(200).IsRequired();
                    m.Property(x => x.Message).HasMaxLength(1000).IsRequired();
                    m.Property(x => x.MeetingDate)
                        .HasColumnType("date")
                        .HasDefaultValueSql("CAST(GETUTCDATE() AS date)");
                    m.Property(x => x.MeetingTime)
                        .HasConversion(
                            v => v.ToTimeSpan(),
                            v => TimeOnly.FromTimeSpan(v))
                        .HasColumnType("time")
                        .HasDefaultValueSql("CAST(GETUTCDATE() AS time)");
                    m.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                });
                // MeetingDepartment linking meetings to departments (many-to-many)
                modelBuilder.Entity<MeetingDepartment>(md =>
                {
                    md.ToTable("MeetingDepartments");
                    md.HasKey(x => new { x.MeetingId, x.DepartmentId });
                    md.HasOne(md => md.Meeting)
                        .WithMany(m => m.MeetingDepartments)
                        .HasForeignKey(md => md.MeetingId)
                        .OnDelete(DeleteBehavior.Cascade);
                    md.HasOne(md => md.Department)
                        .WithMany()
                        .HasForeignKey(md => md.DepartmentId)
                        .OnDelete(DeleteBehavior.Restrict);
                });
                modelBuilder.Entity<MeetingAttachment>(ma =>
                {
                    ma.ToTable("MeetingAttachments");
                    ma.HasKey(x => x.Id);
                    ma.Property(x => x.FileName).HasMaxLength(500).IsRequired();
                    ma.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
                    ma.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
                    ma.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
                    ma.HasIndex(x => x.MeetingId);
                    ma.HasOne(x => x.Meeting)
                        .WithMany(x => x.Attachments)
                        .HasForeignKey(x => x.MeetingId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
        });

        modelBuilder.Entity<FcmToken>(b =>
        {
            b.ToTable("FcmTokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.Token).HasMaxLength(500).IsRequired();
            b.Property(x => x.DeviceInfo).HasMaxLength(200);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<PublicHoliday>(b =>
        {
            b.ToTable("PublicHolidays");
            b.HasKey(x => x.Id);
            b.Property(x => x.Date).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(200);
            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.Year);
            b.HasMany(x => x.Exceptions)
                .WithOne(x => x.PublicHoliday)
                .HasForeignKey(x => x.PublicHolidayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PublicHolidayException>(b =>
        {
            b.ToTable("PublicHolidayExceptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.PublicHolidayId).IsRequired();
            b.HasIndex(x => x.PublicHolidayId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.DepartmentId);
            b.HasIndex(x => x.EmploymentModeId);
            b.HasIndex(x => x.Religion);
            b.HasOne(x => x.PublicHoliday)
                .WithMany(x => x.Exceptions)
                .HasForeignKey(x => x.PublicHolidayId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.EmploymentMode)
                .WithMany()
                .HasForeignKey(x => x.EmploymentModeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InsuranceSettings>(b =>
        {
            b.ToTable("InsuranceSettings");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.EmployeePercentage).HasColumnType("decimal(5,2)").IsRequired();
            b.Property(x => x.CompanyPercentage).HasColumnType("decimal(5,2)").IsRequired();
            b.Property(x => x.MinimumAmount).HasColumnType("decimal(12,2)").IsRequired();
            b.Property(x => x.MaximumAmount).HasColumnType("decimal(12,2)").IsRequired();
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(x => x.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<TaxBracket>(b =>
        {
            b.ToTable("TaxBrackets");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.FromAmount).HasColumnType("decimal(12,2)").IsRequired();
            b.Property(x => x.ToAmount).HasColumnType("decimal(12,2)");
            b.Property(x => x.Percentage).HasColumnType("decimal(5,2)").IsRequired();
            b.Property(x => x.Order).IsRequired();
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(x => x.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.Order);
        });

        modelBuilder.Entity<EmployeePenalty>(b =>
        {
            b.ToTable("EmployeePenalties");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.PenaltyType)
                .HasConversion<int>()
                .IsRequired();
            b.Property(x => x.Days).HasColumnType("decimal(5,2)");
            b.Property(x => x.Amount).HasColumnType("decimal(12,2)");
            b.Property(x => x.PenaltyDate)
                .HasConversion(
                    v => v.ToDateTime(TimeOnly.MinValue),
                    v => DateOnly.FromDateTime(v))
                .IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.IsApplied).IsRequired().HasDefaultValue(false);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.IsApplied);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InsuranceCompany>(b =>
        {
            b.ToTable("InsuranceCompanies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.NameAr).HasMaxLength(200);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<PasswordResetOtp>(b =>
        {
            b.ToTable("PasswordResetOtps");
            b.HasKey(x => x.Id);
            b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.OtpHash).HasMaxLength(500).IsRequired();
            b.Property(x => x.OtpSalt).HasMaxLength(200).IsRequired();
            b.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);
            b.HasIndex(x => x.PhoneNumber);
            b.HasIndex(x => new { x.PhoneNumber, x.IsUsed, x.ExpiresAt });
        });

        modelBuilder.Entity<EmployeeBonus>(b =>
        {
            b.ToTable("EmployeeBonuses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Amount).HasColumnType("decimal(12,2)").IsRequired();
            b.Property(x => x.BonusDate)
                .HasConversion(
                    v => v.ToDateTime(TimeOnly.MinValue),
                    v => DateOnly.FromDateTime(v))
                .IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(x => x.UserId);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeHistory>(b =>
        {
            b.ToTable("EmployeeHistories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            
            // Event Information
            b.Property(x => x.EventType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            b.Property(x => x.EventDate).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            
            // Change Details (JSON fields for flexibility)
            b.Property(x => x.OldValue).HasMaxLength(1000);
            b.Property(x => x.NewValue).HasMaxLength(1000);
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.Notes).HasMaxLength(1000);
            
            // Salary fields
            b.Property(x => x.OldSalary).HasColumnType("decimal(12,2)");
            b.Property(x => x.NewSalary).HasColumnType("decimal(12,2)");
            
            // Effective dates
            b.Property(x => x.EffectiveDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null);
            
            b.Property(x => x.EndDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null);
            
            // Indexes for better query performance
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.EventDate);
            b.HasIndex(x => new { x.EmployeeId, x.EventType });
            
            // Foreign key to Employee
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Foreign key to User who made the change
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.DoneBy)
                .OnDelete(DeleteBehavior.NoAction);
            
            // Foreign key to User who approved
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.ApprovedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SystemErrorLog>(b =>
        {
            b.ToTable("SystemErrorLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(x => x.Path).HasMaxLength(1024);
            b.Property(x => x.Method).HasMaxLength(16);
            b.Property(x => x.QueryString).HasMaxLength(2048);
            b.Property(x => x.Message).HasMaxLength(4000);
            b.Property(x => x.ExceptionType).HasMaxLength(512);
            b.Property(x => x.TraceId).HasMaxLength(128);
            b.Property(x => x.RemoteIp).HasMaxLength(64);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<DeviceSyncState>(b =>
        {
            b.ToTable("DeviceSyncStates");
            b.HasKey(x => x.DeviceKey);
            b.Property(x => x.DeviceKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastSyncedDate)
                .HasColumnType("date");
            b.Property(x => x.LastSyncedAt)
                .HasColumnType("datetime2");
        });

        modelBuilder.Entity<WorkFromHomeRequest>(b =>
        {
            b.ToTable("WorkFromHomeRequests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.HasIndex(x => x.UserId);
        });
    }
}
