using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YGT.DynamicSurvey.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminRequestStatus",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminRequestedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminReviewedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReviewedByUserId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequestedAdminAccess",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminRequestStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AdminRequestedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AdminReviewedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AdminReviewedByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RequestedAdminAccess",
                table: "AspNetUsers");
        }
    }
}
