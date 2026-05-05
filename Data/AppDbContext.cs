using Microsoft.EntityFrameworkCore;
using ElearningAPI.Models; // Đảm bảo khai báo đúng namespace chứa các model của bạn

namespace ElearningAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo 8 bảng tương ứng với 8 model sẽ được tạo trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<TeacherStudent> TeacherStudents { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // 1. Cấu hình khóa chính kép cho bảng trung gian TeacherStudent
    modelBuilder.Entity<TeacherStudent>()
        .HasKey(ts => new { ts.TeacherId, ts.StudentId });

    // 2. Chỉ rõ mối quan hệ: Giáo viên quản lý những học sinh nào
    modelBuilder.Entity<TeacherStudent>()
        .HasOne(ts => ts.Teacher)
        .WithMany(u => u.StudentsManaged) // Map chuẩn xác với danh sách trong class User
        .HasForeignKey(ts => ts.TeacherId)
        .OnDelete(DeleteBehavior.Cascade);

    // 3. Chỉ rõ mối quan hệ: Học sinh được quản lý bởi những giáo viên nào
    modelBuilder.Entity<TeacherStudent>()
        .HasOne(ts => ts.Student)
        .WithMany(u => u.TeachersManagingMe) // Map chuẩn xác với danh sách trong class User
        .HasForeignKey(ts => ts.StudentId)
        .OnDelete(DeleteBehavior.Cascade);

    // 4. Cấu hình thêm các quy tắc xóa an toàn (Tránh lỗi chu kỳ vòng lặp - Multiple cascade paths)
    modelBuilder.Entity<News>()
        .HasOne(n => n.Author)
        .WithMany(u => u.AuthoredNews)
        .HasForeignKey(n => n.AuthorId)
        .OnDelete(DeleteBehavior.SetNull); // Nếu Admin bị xóa, tin bài sẽ giữ lại nhưng không có tác giả
        
    modelBuilder.Entity<Course>()
        .HasOne(c => c.Creator)
        .WithMany(u => u.CreatedCourses)
        .HasForeignKey(c => c.CreatedBy)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<Lesson>()
        .HasOne(l => l.Creator)
        .WithMany(u => u.CreatedLessons)
        .HasForeignKey(l => l.CreatedBy)
        .OnDelete(DeleteBehavior.Restrict); // Không cho xóa User nếu người đó vẫn còn bài giảng đang lưu
}
    }
}