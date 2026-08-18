using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YGT.DynamicSurvey.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicQuestionBranching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "DependsOnQuestionOrder",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShowWhenAnswerEquals",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Category",
                table: "SystemLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_CreatedAt",
                table: "SystemLogs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemLogs_Category",
                table: "SystemLogs");

            migrationBuilder.DropIndex(
                name: "IX_SystemLogs_CreatedAt",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "DependsOnQuestionOrder",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ShowWhenAnswerEquals",
                table: "Questions");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
