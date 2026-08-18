using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YGT.DynamicSurvey.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyOwnerAndUniqueCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_Code",
                table: "Surveys",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Surveys_Code",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Surveys");
        }
    }
}
