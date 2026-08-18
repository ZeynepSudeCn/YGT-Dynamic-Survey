using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YGT.DynamicSurvey.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToSurveyResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SurveyResponses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_UserId",
                table: "SurveyResponses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UserId",
                table: "SurveyResponses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UserId",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_UserId",
                table: "SurveyResponses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SurveyResponses");
        }
    }
}
