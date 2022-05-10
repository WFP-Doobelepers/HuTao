using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddNotificationConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReprimandAppealMessage",
                table: "ModerationRules");

            migrationBuilder.AddColumn<int>(
                name: "NotifyReprimands",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReprimandAppealMessage",
                table: "LoggingRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowAppealOnReprimands",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyReprimands",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ReprimandAppealMessage",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ShowAppealOnReprimands",
                table: "LoggingRules");

            migrationBuilder.AddColumn<string>(
                name: "ReprimandAppealMessage",
                table: "ModerationRules",
                type: "text",
                nullable: true);
        }
    }
}