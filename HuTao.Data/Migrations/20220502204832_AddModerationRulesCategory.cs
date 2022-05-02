using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddModerationRulesCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_Guilds_GuildId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationRules_Guilds_GuildId",
                table: "ModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_ModerationRules_GuildId",
                table: "ModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_ModerationLoggingRules_GuildId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ModerationLoggingRules");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CensorTimeRange",
                table: "ModerationCategory",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LoggingRulesId",
                table: "ModerationCategory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MuteRoleId",
                table: "ModerationCategory",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "NoticeExpiryLength",
                table: "ModerationCategory",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReplaceMutes",
                table: "ModerationCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WarningExpiryLength",
                table: "ModerationCategory",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationLoggingRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationCategoryId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationCategory_LoggingRulesId",
                table: "ModerationCategory",
                column: "LoggingRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_ModerationLoggingRulesId",
                table: "Guilds",
                column: "ModerationLoggingRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_ModerationRulesId",
                table: "Guilds",
                column: "ModerationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_ModerationCategoryId",
                table: "Criterion",
                column: "ModerationCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_ModerationCategory_ModerationCategoryId",
                table: "Criterion",
                column: "ModerationCategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_ModerationLoggingRules_ModerationLoggingRulesId",
                table: "Guilds",
                column: "ModerationLoggingRulesId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_ModerationRules_ModerationRulesId",
                table: "Guilds",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingRulesId",
                table: "ModerationCategory",
                column: "LoggingRulesId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_ModerationCategory_ModerationCategoryId",
                table: "Criterion");

            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_ModerationLoggingRules_ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_ModerationRules_ModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingRulesId",
                table: "ModerationCategory");

            migrationBuilder.DropIndex(
                name: "IX_ModerationCategory_LoggingRulesId",
                table: "ModerationCategory");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_ModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_ModerationCategoryId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "CensorTimeRange",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "LoggingRulesId",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "MuteRoleId",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "NoticeExpiryLength",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "ReplaceMutes",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "WarningExpiryLength",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "ModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "ModerationCategoryId",
                table: "Criterion");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "ModerationRules",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "ModerationLoggingRules",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRules_GuildId",
                table: "ModerationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_GuildId",
                table: "ModerationLoggingRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_Guilds_GuildId",
                table: "ModerationLoggingRules",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationRules_Guilds_GuildId",
                table: "ModerationRules",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
