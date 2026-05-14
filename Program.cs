using ElearningAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
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

    if (!await db.Courses.AnyAsync(c => c.Title == "Sinh học 12"))
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN);
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
        }

        var teacher = await db.Users.FirstOrDefaultAsync(u => u.Email == "thuyet.bio12@edusmart.vn");
        if (teacher == null)
        {
            teacher = new User
            {
                FullName = "Nguyễn Viết Thuyết",
                Email = "thuyet.bio12@edusmart.vn",
                PasswordHash = "12345678",
                Role = UserRole.TEACHER,
                AvatarUrl = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=256&q=80",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(teacher);
            await db.SaveChangesAsync();
        }

        var student = await db.Users.FirstOrDefaultAsync(u => u.Email == "student.bio12@edusmart.vn");
        if (student == null)
        {
            student = new User
            {
                FullName = "Học viên Sinh học 12",
                Email = "student.bio12@edusmart.vn",
                PasswordHash = "12345678",
                Role = UserRole.STUDENT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(student);
            await db.SaveChangesAsync();
        }

        var course = new Course
        {
            Title = "Sinh học 12",
            Description = "<p>Khóa học Sinh học 12 được biên soạn bám sát chương trình giáo dục phổ thông mới. Học sinh sẽ được học toàn bộ kiến thức từ cơ bản đến nâng cao, kết hợp lý thuyết, bài tập, video minh họa, flashcard và quiz giúp luyện thi tốt nghiệp và đại học hiệu quả.</p>",
            AvatarUrl = "https://images.unsplash.com/photo-1530210124550-912dc1381cb8?auto=format&fit=crop&w=1200&q=80",
            Code = "SINH-HOC-12",
            IntroVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Category = "Sinh học 12",
            Status = "Published",
            Level = "Nâng cao",
            Language = "Tiếng Việt",
            DurationMinutes = 2900,
            ExpectedStudentCount = 1248,
            StartDate = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc),
            LearningOutcomes = "Hệ thống kiến thức đầy đủ, dễ hiểu\nBài giảng video chất lượng cao\nTài liệu PDF và sơ đồ tư duy\nFlashcard giúp ghi nhớ nhanh\nBài tập, quiz và bài thi đánh giá năng lực",
            CreatedBy = admin.Id,
            TeacherId = teacher.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var lessonTitles = new[]
        {
            "Giới thiệu chung về sinh học",
            "Các cấp độ tổ chức của thế giới sống",
            "Thành phần hóa học của tế bào",
            "Cấu trúc và chức năng tế bào",
            "Di truyền học",
            "Biến dị",
            "Tiến hóa",
            "Sinh thái học",
            "Ứng dụng sinh học"
        };

        foreach (var title in lessonTitles)
        {
            db.Lessons.Add(new Lesson
            {
                CourseId = course.Id,
                Title = title,
                Description = $"Nội dung bài học: {title}.",
                VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                PdfUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf",
                CreatedBy = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        db.Enrollments.Add(new Enrollment
        {
            CourseId = course.Id,
            StudentId = student.Id,
            ProgressPercentage = 68,
            EnrolledAt = DateTime.UtcNow.AddDays(-7),
            LastAccessed = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    var demoCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Sinh học 12" || c.Code == "SINH-HOC-12");
    if (demoCourse != null && string.IsNullOrWhiteSpace(demoCourse.Code))
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN);
        var teacher = await db.Users.FirstOrDefaultAsync(u => u.Email == "thuyet.bio12@edusmart.vn");

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
        }

        if (teacher == null)
        {
            teacher = new User
            {
                FullName = "Nguyễn Viết Thuyết",
                Email = "thuyet.bio12@edusmart.vn",
                PasswordHash = "12345678",
                Role = UserRole.TEACHER,
                AvatarUrl = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=256&q=80",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(teacher);
            await db.SaveChangesAsync();
        }

        demoCourse.Code = "SINH-HOC-12";
        demoCourse.Description = "<p>Khóa học Sinh học 12 được biên soạn bám sát chương trình giáo dục phổ thông mới. Học sinh sẽ được học toàn bộ kiến thức từ cơ bản đến nâng cao, kết hợp lý thuyết, bài tập, video minh họa, flashcard và quiz giúp luyện thi tốt nghiệp và đại học hiệu quả.</p>";
        demoCourse.AvatarUrl = "https://images.unsplash.com/photo-1530210124550-912dc1381cb8?auto=format&fit=crop&w=1200&q=80";
        demoCourse.IntroVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        demoCourse.Category = "Sinh học 12";
        demoCourse.Status = "Published";
        demoCourse.Level = "Nâng cao";
        demoCourse.Language = "Tiếng Việt";
        demoCourse.DurationMinutes = 2900;
        demoCourse.ExpectedStudentCount = 1248;
        demoCourse.StartDate = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        demoCourse.EndDate = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);
        demoCourse.LearningOutcomes = "Hệ thống kiến thức đầy đủ, dễ hiểu\nBài giảng video chất lượng cao\nTài liệu PDF và sơ đồ tư duy\nFlashcard giúp ghi nhớ nhanh\nBài tập, quiz và bài thi đánh giá năng lực";
        demoCourse.TeacherId = teacher.Id;
        demoCourse.UpdatedAt = DateTime.UtcNow;

        var lessonCount = await db.Lessons.CountAsync(l => l.CourseId == demoCourse.Id);
        if (lessonCount < 8)
        {
            var lessonTitles = new[] { "Giới thiệu chung về sinh học", "Các cấp độ tổ chức của thế giới sống", "Thành phần hóa học của tế bào", "Cấu trúc và chức năng tế bào", "Di truyền học", "Biến dị", "Tiến hóa", "Sinh thái học" };
            foreach (var title in lessonTitles.Skip(lessonCount))
            {
                db.Lessons.Add(new Lesson
                {
                    CourseId = demoCourse.Id,
                    Title = title,
                    Description = $"Nội dung bài học: {title}.",
                    VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    PdfUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf",
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
app.UseStaticFiles(); // Cho phép truy cập file tĩnh trong wwwroot
app.UseCors("AllowReactApp");

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseAuthentication(); // Xác thực danh tính
app.UseAuthorization();  // Phân quyền truy cập
// Map controller routes
app.MapControllers();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Health check endpoint cho UptimeRobot/Cron-job
app.MapGet("/api/health", () => Results.Ok("OK"));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
