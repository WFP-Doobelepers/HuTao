using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class ModerationEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarningCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AutoModerationRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AntiSpamRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuplicateTolerance = table.Column<int>(type: "integer", nullable: true),
                    DuplicateMessageTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    MessageSpamTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EmojiLimit = table.Column<long>(type: "bigint", nullable: true),
                    MessageLimit = table.Column<long>(type: "bigint", nullable: true),
                    NewlineLimit = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    GuildUserEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warning", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warning_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Warning_Users_GuildUserEntityId",
                        column: x => x.GuildUserEntityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warning_Users_ModeratorId",
                        column: x => x.ModeratorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warning_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoModerationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AntiSpamRulesId = table.Column<Guid>(type: "uuid", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "WarningTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reprimand = table.Column<int>(type: "integer", nullable: false),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_Guilds_AutoModerationRulesId",
                table: "Guilds",
                column: "AutoModerationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_AntiSpamRulesId",
                table: "AutoModerationRules",
                column: "AntiSpamRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildId",
                table: "Warning",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildUserEntityId",
                table: "Warning",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_ModeratorId",
                table: "Warning",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_UserId",
                table: "Warning",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarningTrigger_AutoModerationRulesId",
                table: "WarningTrigger",
                column: "AutoModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AutoModerationRules_AutoModerationRulesId",
                table: "Guilds",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AutoModerationRules_AutoModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropTable(
                name: "Warning");

            migrationBuilder.DropTable(
                name: "WarningTrigger");

            migrationBuilder.DropTable(
                name: "AutoModerationRules");

            migrationBuilder.DropTable(
                name: "AntiSpamRules");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_AutoModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AutoModerationRulesId",
                table: "Guilds");
        }
    }
}
