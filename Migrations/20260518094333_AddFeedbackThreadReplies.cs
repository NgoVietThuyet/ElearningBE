using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElearningAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackThreadReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "Feedbacks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentFeedbackId",
                table: "Feedbacks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_AuthorId",
                table: "Feedbacks",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ParentFeedbackId",
                table: "Feedbacks",
                column: "ParentFeedbackId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Feedbacks_ParentFeedbackId",
                table: "Feedbacks",
                column: "ParentFeedbackId",
                principalTable: "Feedbacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Users_AuthorId",
                table: "Feedbacks",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Feedbacks_ParentFeedbackId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Users_AuthorId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_AuthorId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ParentFeedbackId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ParentFeedbackId",
                table: "Feedbacks");
        }
    }
}
