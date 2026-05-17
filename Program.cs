using ElearningAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ElearningAPI.Models;
using ElearningAPI.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Tăng giới hạn kích thước header lên 64KB để tránh lỗi 431 khi JWT token lớn
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 65536; // 64 KB
    options.Limits.MaxRequestHeaderCount = 100;
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Giữ nguyên ký tự Tiếng Việt trong JSON response (không escape sang \uXXXX)
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

// Read and validate JWT configuration before wiring up authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
    throw new InvalidOperationException("JWT configuration is missing or incomplete in appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();

// 1. Định nghĩa chính sách CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                  {
                      if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                      return uri.Host == "localhost"
                          || uri.Host == "127.0.0.1"
                          || uri.Host == "elearning-fe-jcuz.vercel.app"
                          || uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
                  })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Tự động chạy migration khi khởi động (bao gồm reset passwords và xóa base64 avatar)
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
            logger.LogWarning(ex, "Database migration failed on attempt {Attempt}. Retrying...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
        }
    }

    // 1. Tự động dọn sạch toàn bộ Mock Data khi phát hiện các dấu hiệu dữ liệu mẫu cũ
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
        u.Email.StartsWith("teacher") && u.Email.EndsWith("@edusmart.com") || 
        u.Email.StartsWith("student") && u.Email.EndsWith("@student.com")
    );

    if (hasMockCourses || hasMockUsers)
    {
        try
        {
            logger.LogInformation("CLEANUP: Phát hiện mock data trong hệ thống. Đang tiến hành xóa sạch dữ liệu mẫu...");

            // Xóa toàn bộ dữ liệu ở các bảng liên kết
            db.TestResults.RemoveRange(db.TestResults);
            db.Tests.RemoveRange(db.Tests);
            db.Enrollments.RemoveRange(db.Enrollments);
            db.Lessons.RemoveRange(db.Lessons);
            db.CourseMaterials.RemoveRange(db.CourseMaterials);
            db.Feedbacks.RemoveRange(db.Feedbacks);
            db.Courses.RemoveRange(db.Courses);
            db.TeacherStudents.RemoveRange(db.TeacherStudents);
            db.News.RemoveRange(db.News);

            // Xóa tất cả người dùng không phải ADMIN
            var nonAdmins = db.Users.Where(u => u.Role != UserRole.ADMIN);
            db.Users.RemoveRange(nonAdmins);

            await db.SaveChangesAsync();
            logger.LogInformation("CLEANUP: Đã xóa thành công toàn bộ dữ liệu mẫu khỏi database.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CLEANUP: Gặp lỗi khi xóa dữ liệu mẫu.");
        }
    }

    // 2. Đảm bảo luôn tồn tại ít nhất 1 tài khoản ADMIN để đăng nhập quản trị
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN);
    if (admin == null)
    {
        admin = new User
        {
            FullName = "Người dùng",
            Email = "admin@edusmart.vn",
            PasswordHash = "12345678", // AuthService hỗ trợ đăng nhập bằng plaintext
            Role = UserRole.ADMIN,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("ADMIN: Đã tạo tài khoản quản trị mặc định 'admin@edusmart.vn' thành công.");
    }
}

// Configure the HTTP request pipeline.
app.UseStaticFiles(); // Cho phép truy cập file tĩnh trong wwwroot
app.UseCors("AllowReactApp");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // Xác thực danh tính
app.UseAuthorization();  // Phân quyền truy cập

// Map controller routes
app.MapControllers();

// Health check endpoint cho UptimeRobot/Cron-job
app.MapGet("/api/health", () => Results.Ok("OK"));

app.Run();
