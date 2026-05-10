using internalEmployee.Auth;
using internalEmployee.Services.Meeting;
using internalEmployee.Data;
using internalEmployee.Services.Auth;
using internalEmployee.Services.Home;
using internalEmployee.Services.Permission;
using internalEmployee.Services.Leave;
using internalEmployee.Services.Assignment;
using internalEmployee.Services.AdminDashboard;
using internalEmployee.Services.SuperAdminDashboard;
using internalEmployee.Services.Notification;
using internalEmployee.Services.Attachment;
using internalEmployee.Services.PublicHoliday;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using internalEmployee.Services.Attendance;
using internalEmployee.Services.Penalty;
using internalEmployee.Middleware;
using System.IdentityModel.Tokens.Jwt;
using internalEmployee.Services.UserLocation;
using internalEmployee.Services.MediconsultHr;
using internalEmployee.Services.Custody;
using internalEmployee.Services.Recruitment;
using internalEmployee.Services.WorkFromHome;


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Internal Employee API",
        Version = "v1"
    });

    // Map DateOnly and TimeOnly to string for Swagger
    options.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "date"
    });
    options.MapType<DateOnly?>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Nullable = true
    });
    options.MapType<TimeOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "time"
    });
    options.MapType<TimeOnly?>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "time",
        Nullable = true
    });

    // Custom schema IDs to avoid conflicts
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    
    // Add schema filter FIRST to handle UpdateProfileRequest properly (prevents errors with DateOnly? and List<string>?)
    // This must run before Swagger tries to process the type
    options.SchemaFilter<internalEmployee.Data.ExcludeUpdateProfileRequestFilter>();
    
    // Add operation filter to handle form data properly
    options.OperationFilter<internalEmployee.Data.FormDataOperationFilter>();
    
    // Add document filter to clean up any problematic schemas
    options.DocumentFilter<internalEmployee.Data.SwaggerDocumentFilter>();
    
    // Ignore any schema generation errors to prevent Swagger from crashing
    options.IgnoreObsoleteActions();

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Auth:Jwt"));
builder.Services.Configure<CompanyLocationOptions>(builder.Configuration.GetSection("CompanyLocation"));
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ISuperAdminDashboardService, SuperAdminDashboardService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<internalEmployee.Services.Overtime.IOvertimeService, internalEmployee.Services.Overtime.OvertimeService>();
builder.Services.AddScoped<internalEmployee.Services.SalaryAdvance.ISalaryAdvanceService, internalEmployee.Services.SalaryAdvance.SalaryAdvanceService>();
builder.Services.AddScoped<internalEmployee.Services.HealthInsurance.IHealthInsuranceService, internalEmployee.Services.HealthInsurance.HealthInsuranceService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IUserLocationService, UserLocationService>();
builder.Services.AddScoped<ICustodyService, CustodyService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IUserAttachmentService, UserAttachmentService>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<IPenaltyService, PenaltyService>();
builder.Services.AddScoped<internalEmployee.Services.Bonus.IBonusService, internalEmployee.Services.Bonus.BonusService>();
builder.Services.AddScoped<internalEmployee.Services.InsuranceCompany.IInsuranceCompanyService, internalEmployee.Services.InsuranceCompany.InsuranceCompanyService>();
builder.Services.AddScoped<internalEmployee.Services.EmployeeHistory.IEmployeeHistoryService, internalEmployee.Services.EmployeeHistory.EmployeeHistoryService>();
builder.Services.AddHttpClient<internalEmployee.Services.ZKTeco.IZKTecoService, internalEmployee.Services.ZKTeco.ZKTecoService>();
builder.Services.AddHttpClient<IMediconsultHrService, MediconsultHrService>();
builder.Services.AddScoped<IWorkFromHomeService, WorkFromHomeService>();



builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Disable automatic mapping to ensure "role" claim is not changed

        var jwt = builder.Configuration.GetSection("Auth:Jwt").Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Missing configuration section Auth:Jwt");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();

// Add CORS with AllowAll
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure DB is created/migrated & seed reference data (Departments, etc.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
   // await DbSeeder.SeedAsync(db, CancellationToken.None);
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments (Development and Production)
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Enable CORS (must be before UseAuthentication and UseAuthorization)
app.UseCors();

// Enable static files for serving uploaded images
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Enable automatic employee history tracking (must be after UseAuthentication)
app.UseErrorDbLogging();
app.UseStatusCodeDbLogging();
app.UseEmployeeHistoryTracking();


app.MapControllers();

app.Run();
