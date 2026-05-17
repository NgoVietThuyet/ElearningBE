using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeArVrResourceToLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "ArVrUrl",
                table: "Lessons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArVrUrl",
                table: "Lessons");

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
        }
    }
}
