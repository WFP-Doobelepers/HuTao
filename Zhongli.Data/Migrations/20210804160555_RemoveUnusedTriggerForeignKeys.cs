using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class RemoveUnusedTriggerForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "KickTriggerId",
                table: "AutoModerationRules");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BanTriggerId",
                table: "AutoModerationRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KickTriggerId",
                table: "AutoModerationRules",
                type: "uuid",
                nullable: true);
        }
    }
}
