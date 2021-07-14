using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class NoticeEntitySeparation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_WarningTriggers_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_WarningTriggers_KickTriggerId",
                table: "AutoModerationRules");

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
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "Retroactive",
                table: "WarningTriggers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "WarningCount",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "NoticeCount",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "NoticeTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Retroactive = table.Column<bool>(type: "boolean", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoticeTrigger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoticeTrigger_AutoModerationRules_AutoModerationRulesId",
                        column: x => x.AutoModerationRulesId,
                        principalTable: "AutoModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NoticeTrigger_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoticeTrigger_ActionId",
                table: "NoticeTrigger",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_NoticeTrigger_AutoModerationRulesId",
                table: "NoticeTrigger",
                column: "AutoModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropTable(
                name: "NoticeTrigger");

            migrationBuilder.DropColumn(
                name: "Retroactive",
                table: "WarningTriggers");

            migrationBuilder.AlterColumn<Guid>(
                name: "AutoModerationRulesId",
                table: "WarningTriggers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarningCount",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "NoticeCount",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

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
                name: "FK_AutoModerationRules_WarningTriggers_BanTriggerId",
                table: "AutoModerationRules",
                column: "BanTriggerId",
                principalTable: "WarningTriggers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoModerationRules_WarningTriggers_KickTriggerId",
                table: "AutoModerationRules",
                column: "KickTriggerId",
                principalTable: "WarningTriggers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
