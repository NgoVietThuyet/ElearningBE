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

// SSE — Singleton để dùng chung state across requests
builder.Services.AddSingleton<ISseConnectionManager, SseConnectionManager>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<DocumentConversionService>();
builder.Services.AddScoped<IPdfQuizParserService, PdfQuizParserService>();

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
    // CREATE ADMIN
    // ================================
    if (!await db.Users.AnyAsync(u => u.Email == "diem@gmail.com"))
    {
        db.Users.Add(new User
        {
            FullName = "Admin GenZBio",
            Email = "diem@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("diem123"),
            Role = UserRole.ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation("ADMIN: Đã tạo tài khoản diem@gmail.com.");
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