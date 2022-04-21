using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddTriggerRepeatMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Retroactive",
                table: "WarningTriggers");

            migrationBuilder.DropColumn(
                name: "Retroactive",
                table: "NoticeTrigger");

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "WarningTriggers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "NoticeTrigger",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "WarningTriggers");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "NoticeTrigger");

            migrationBuilder.AddColumn<bool>(
                name: "Retroactive",
                table: "WarningTriggers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retroactive",
                table: "NoticeTrigger",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}