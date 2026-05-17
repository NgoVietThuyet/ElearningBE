using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherLessonResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArVrContentType",
                table: "Lessons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ArVrFile",
                table: "Lessons",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArVrFileName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonPlanContentType",
                table: "Lessons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LessonPlanFile",
                table: "Lessons",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonPlanFileName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlideContentType",
                table: "Lessons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SlideFile",
                table: "Lessons",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlideFileName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArVrContentType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ArVrFile",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ArVrFileName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonPlanContentType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonPlanFile",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonPlanFileName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SlideContentType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SlideFile",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SlideFileName",
                table: "Lessons");
        }
    }
}
