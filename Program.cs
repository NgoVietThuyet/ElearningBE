using ElearningAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ElearningAPI.Models;
using ElearningAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ================================
// KESTREL
// ================================
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 65536;
    options.Limits.MaxRequestHeaderCount = 100;
});

// ================================
// CONTROLLERS
// ================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder =
            System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================================
// DATABASE
// ================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)
    ));

// ================================
// JWT
// ================================
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) ||
    string.IsNullOrEmpty(jwtIssuer) ||
    string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is missing or incomplete in appsettings."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                )
        };
    });

// ================================
// CORS
// ================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "https://elearning-fe-jcuz.vercel.app",
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ================================
// SERVICES
// ================================
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();

var app = builder.Build();

// ================================
// AUTO MIGRATION
// ================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < 5)
        {
            logger.LogWarning(
                ex,
                "Database migration failed on attempt {Attempt}. Retrying...",
                attempt
            );

            await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
        }
    }

    // ================================
    // CLEANUP MOCK DATA
    // ================================
    bool hasMockCourses = await db.Courses.AnyAsync(c =>
        c.Title == "Sinh học 12" ||
        c.Code == "SINH-HOC-12" ||
        c.Title.StartsWith("Sinh học Phân tử") ||
        c.Title.StartsWith("Di truyền học Lâm sàng") ||
        c.Title.StartsWith("Sinh thái và Môi trường")
    );

    bool hasMockUsers = await db.Users.AnyAsync(u =>
        u.Email == "thuyet.bio12@edusmart.vn" ||
        u.Email == "student.bio12@edusmart.vn" ||
        (u.Email.StartsWith("teacher") &&
         u.Email.EndsWith("@edusmart.com")) ||
        (u.Email.StartsWith("student") &&
         u.Email.EndsWith("@student.com"))
    );

    if (hasMockCourses || hasMockUsers)
    {
        try
        {
            logger.LogInformation(
                "CLEANUP: Phát hiện mock data. Đang xóa..."
            );

            db.TestResults.RemoveRange(db.TestResults);
            db.Tests.RemoveRange(db.Tests);
            db.Enrollments.RemoveRange(db.Enrollments);
            db.Lessons.RemoveRange(db.Lessons);
            db.CourseMaterials.RemoveRange(db.CourseMaterials);
            db.Feedbacks.RemoveRange(db.Feedbacks);
            db.Courses.RemoveRange(db.Courses);
            db.TeacherStudents.RemoveRange(db.TeacherStudents);
            db.News.RemoveRange(db.News);

            var nonAdmins =
                db.Users.Where(u => u.Role != UserRole.ADMIN);

            db.Users.RemoveRange(nonAdmins);

            await db.SaveChangesAsync();

            logger.LogInformation(
                "CLEANUP: Đã xóa mock data thành công."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "CLEANUP: Lỗi khi xóa mock data."
            );
        }
    }

    // ================================
    // CREATE DEFAULT ADMIN
    // ================================
    var admin = await db.Users
        .FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN);

    if (admin == null)
    {
        admin = new User
        {
            FullName = "Người dùng",
            Email = "admin@edusmart.vn",
            PasswordHash = "12345678",
            Role = UserRole.ADMIN,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);

        await db.SaveChangesAsync();

        logger.LogInformation(
            "ADMIN: Đã tạo tài khoản admin mặc định."
        );
    }
}

// ================================
// MIDDLEWARE
// ================================
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

// QUAN TRỌNG: CORS PHẢI ĐỨNG TRƯỚC AUTH
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

// ================================
// ROUTES
// ================================
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok("OK"));

app.Run();