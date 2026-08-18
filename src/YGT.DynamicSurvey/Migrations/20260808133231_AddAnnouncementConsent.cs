using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YGT.DynamicSurvey.Migrations
{
    public partial class AddAnnouncementConsent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnnouncementConsent",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnouncementConsent",
                table: "AspNetUsers");
        }
    }
}