using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class ResetPasswordsAndClearBase64Avatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset mật khẩu về plain text cho các tài khoản cụ thể
            migrationBuilder.Sql("UPDATE \"Users\" SET \"PasswordHash\" = '12345678' WHERE \"Email\" = 'thuyet@gmail.com'");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"PasswordHash\" = 'ly1234' WHERE \"Email\" = 'ly@gmail.com'");

            // Xóa tất cả avatar lưu dạng base64 (gây JWT token ~1MB → lỗi 431)
            migrationBuilder.Sql("UPDATE \"Users\" SET \"AvatarUrl\" = NULL WHERE \"AvatarUrl\" IS NOT NULL AND \"AvatarUrl\" LIKE 'data:%'");
            migrationBuilder.Sql("UPDATE \"Courses\" SET \"AvatarUrl\" = NULL WHERE \"AvatarUrl\" IS NOT NULL AND \"AvatarUrl\" LIKE 'data:%'");
            migrationBuilder.Sql("UPDATE \"News\" SET \"AvatarUrl\" = NULL WHERE \"AvatarUrl\" IS NOT NULL AND \"AvatarUrl\" LIKE 'data:%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không thể khôi phục dữ liệu đã xóa
        }
    }
}
