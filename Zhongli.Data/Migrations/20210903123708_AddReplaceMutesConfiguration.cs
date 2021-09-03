using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddReplaceMutesConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarningAutoPardonLength",
                table: "ModerationRules",
                newName: "WarningExpiryLength");

            migrationBuilder.RenameColumn(
                name: "NoticeAutoPardonLength",
                table: "ModerationRules",
                newName: "NoticeExpiryLength");

            migrationBuilder.AddColumn<bool>(
                name: "ReplaceMutes",
                table: "ModerationRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplaceMutes",
                table: "ModerationRules");

            migrationBuilder.RenameColumn(
                name: "WarningExpiryLength",
                table: "ModerationRules",
                newName: "WarningAutoPardonLength");

            migrationBuilder.RenameColumn(
                name: "NoticeExpiryLength",
                table: "ModerationRules",
                newName: "NoticeAutoPardonLength");
        }
    }
}
