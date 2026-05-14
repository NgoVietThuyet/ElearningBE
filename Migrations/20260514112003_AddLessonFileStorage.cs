using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonFileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentContentType",
                table: "Lessons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "DocumentFile",
                table: "Lessons",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "Lessons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfContentType",
                table: "Lessons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PdfFile",
                table: "Lessons",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFileName",
                table: "Lessons",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "DocumentFile",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PdfContentType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PdfFile",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PdfFileName",
                table: "Lessons");
        }
    }
}
