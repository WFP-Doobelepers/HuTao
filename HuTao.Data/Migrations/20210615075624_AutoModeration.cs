using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AutoModeration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarningTrigger");

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

            migrationBuilder.CreateTable(
                name: "BanTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: false),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanTrigger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KickTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KickTrigger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MuteTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuteTrigger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuteTrigger_AutoModerationRules_AutoModerationRulesId",
                        column: x => x.AutoModerationRulesId,
                        principalTable: "AutoModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules",
                column: "BanTriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules",
                column: "KickTriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_MuteTrigger_AutoModerationRulesId",
                table: "MuteTrigger",
                column: "AutoModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoModerationRules_BanTrigger_BanTriggerId",
                table: "AutoModerationRules",
                column: "BanTriggerId",
                principalTable: "BanTrigger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoModerationRules_KickTrigger_KickTriggerId",
                table: "AutoModerationRules",
                column: "KickTriggerId",
                principalTable: "KickTrigger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_BanTrigger_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_KickTrigger_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropTable(
                name: "BanTrigger");

            migrationBuilder.DropTable(
                name: "KickTrigger");

            migrationBuilder.DropTable(
                name: "MuteTrigger");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.CreateTable(
                name: "WarningTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reprimand = table.Column<int>(type: "integer", nullable: false),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarningTrigger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarningTrigger_AutoModerationRules_AutoModerationRulesId",
                        column: x => x.AutoModerationRulesId,
                        principalTable: "AutoModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarningTrigger_AutoModerationRulesId",
                table: "WarningTrigger",
                column: "AutoModerationRulesId");
        }
    }
}