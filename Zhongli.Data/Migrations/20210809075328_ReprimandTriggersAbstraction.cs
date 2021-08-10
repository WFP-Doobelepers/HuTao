using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class ReprimandTriggersAbstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_ModerationAction_ActionId",
                table: "WarningTriggers");

            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_ModerationRules_ModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropTable(
                name: "NoticeTrigger");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WarningTriggers",
                table: "WarningTriggers");

            migrationBuilder.RenameTable(
                name: "WarningTriggers",
                newName: "Trigger");

            migrationBuilder.RenameIndex(
                name: "IX_WarningTriggers_ModerationRulesId",
                table: "Trigger",
                newName: "IX_Trigger_ModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_WarningTriggers_ActionId",
                table: "Trigger",
                newName: "IX_Trigger_ActionId");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Trigger",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trigger",
                table: "Trigger",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ModerationRules_ModerationRulesId",
                table: "Trigger",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationRules_ModerationRulesId",
                table: "Trigger");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trigger",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Trigger");

            migrationBuilder.RenameTable(
                name: "Trigger",
                newName: "WarningTriggers");

            migrationBuilder.RenameIndex(
                name: "IX_Trigger_ModerationRulesId",
                table: "WarningTriggers",
                newName: "IX_WarningTriggers_ModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_Trigger_ActionId",
                table: "WarningTriggers",
                newName: "IX_WarningTriggers_ActionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WarningTriggers",
                table: "WarningTriggers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "NoticeTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    ModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoticeTrigger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoticeTrigger_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NoticeTrigger_ModerationRules_ModerationRulesId",
                        column: x => x.ModerationRulesId,
                        principalTable: "ModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoticeTrigger_ActionId",
                table: "NoticeTrigger",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_NoticeTrigger_ModerationRulesId",
                table: "NoticeTrigger",
                column: "ModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_ModerationAction_ActionId",
                table: "WarningTriggers",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarningTriggers_ModerationRules_ModerationRulesId",
                table: "WarningTriggers",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
