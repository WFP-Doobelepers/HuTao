using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddReprimandAppealMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Censor_AutoModerationRules_AutoModerationRulesId",
                table: "Censor");

            migrationBuilder.DropForeignKey(
                name: "FK_NoticeTrigger_AutoModerationRules_AutoModerationRulesId",
                table: "NoticeTrigger");

            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropTable(
                name: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "MuteRoleId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "NoticeAutoPardonLength",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WarningAutoPardonLength",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "AutoModerationRulesId",
                table: "WarningTriggers",
                newName: "ModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_WarningTriggers_AutoModerationRulesId",
                table: "WarningTriggers",
                newName: "IX_WarningTriggers_ModerationRulesId");

            migrationBuilder.RenameColumn(
                name: "AutoModerationRulesId",
                table: "NoticeTrigger",
                newName: "ModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_NoticeTrigger_AutoModerationRulesId",
                table: "NoticeTrigger",
                newName: "IX_NoticeTrigger_ModerationRulesId");

            migrationBuilder.RenameColumn(
                name: "AutoModerationRulesId",
                table: "Censor",
                newName: "ModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_Censor_AutoModerationRulesId",
                table: "Censor",
                newName: "IX_Censor_ModerationRulesId");

            migrationBuilder.CreateTable(
                name: "ModerationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AntiSpamRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    MuteRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    NoticeAutoPardonLength = table.Column<TimeSpan>(type: "interval", nullable: true),
                    WarningAutoPardonLength = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ReprimandAppealMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationRules_AntiSpamRules_AntiSpamRulesId",
                        column: x => x.AntiSpamRulesId,
                        principalTable: "AntiSpamRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModerationRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRules_AntiSpamRulesId",
                table: "ModerationRules",
                column: "AntiSpamRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRules_GuildId",
                table: "ModerationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Censor_ModerationRules_ModerationRulesId",
                table: "Censor",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NoticeTrigger_ModerationRules_ModerationRulesId",
                table: "NoticeTrigger",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Censor_ModerationRules_ModerationRulesId",
                table: "Censor");

            migrationBuilder.DropForeignKey(
                name: "FK_NoticeTrigger_ModerationRules_ModerationRulesId",
                table: "NoticeTrigger");

            migrationBuilder.DropForeignKey(
                name: "FK_WarningTriggers_ModerationRules_ModerationRulesId",
                table: "WarningTriggers");

            migrationBuilder.DropTable(
                name: "ModerationRules");

            migrationBuilder.RenameColumn(
                name: "ModerationRulesId",
                table: "WarningTriggers",
                newName: "AutoModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_WarningTriggers_ModerationRulesId",
                table: "WarningTriggers",
                newName: "IX_WarningTriggers_AutoModerationRulesId");

            migrationBuilder.RenameColumn(
                name: "ModerationRulesId",
                table: "NoticeTrigger",
                newName: "AutoModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_NoticeTrigger_ModerationRulesId",
                table: "NoticeTrigger",
                newName: "IX_NoticeTrigger_AutoModerationRulesId");

            migrationBuilder.RenameColumn(
                name: "ModerationRulesId",
                table: "Censor",
                newName: "AutoModerationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_Censor_ModerationRulesId",
                table: "Censor",
                newName: "IX_Censor_AutoModerationRulesId");

            migrationBuilder.AddColumn<decimal>(
                name: "MuteRoleId",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "NoticeAutoPardonLength",
                table: "Guilds",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WarningAutoPardonLength",
                table: "Guilds",
                type: "interval",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AutoModerationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AntiSpamRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoModerationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoModerationRules_AntiSpamRules_AntiSpamRulesId",
                        column: x => x.AntiSpamRulesId,
                        principalTable: "AntiSpamRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoModerationRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_AntiSpamRulesId",
                table: "AutoModerationRules",
                column: "AntiSpamRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_GuildId",
                table: "AutoModerationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Censor_AutoModerationRules_AutoModerationRulesId",
                table: "Censor",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NoticeTrigger_AutoModerationRules_AutoModerationRulesId",
                table: "NoticeTrigger",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
