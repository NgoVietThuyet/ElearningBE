using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCreationFlowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExpectedStudentCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IntroVideoUrl",
                table: "Courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ExpectedStudentCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IntroVideoUrl",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Courses");
        }
    }
}
