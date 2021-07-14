using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class NoticeEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.AlterColumn<Guid>(
                name: "AutoModerationRulesId",
                table: "WarningTriggers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticeCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules",
                column: "BanTriggerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules",
                column: "KickTriggerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "NoticeCount",
                table: "Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "AutoModerationRulesId",
                table: "WarningTriggers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules",
                column: "BanTriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules",
                column: "KickTriggerId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
