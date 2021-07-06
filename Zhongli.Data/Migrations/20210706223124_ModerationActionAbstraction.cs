using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class ModerationActionAbstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_BanTrigger_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_KickTrigger_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropTable(
                name: "Ban");

            migrationBuilder.DropTable(
                name: "BanTrigger");

            migrationBuilder.DropTable(
                name: "Kick");

            migrationBuilder.DropTable(
                name: "KickTrigger");

            migrationBuilder.DropTable(
                name: "Mute");

            migrationBuilder.DropTable(
                name: "MuteTrigger");

            migrationBuilder.DropTable(
                name: "Warning");

            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "AuthorizationRule",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModerationAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationAction_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationAction_Users_ModeratorId_GuildId",
                        columns: x => new { x.ModeratorId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReprimandAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReprimandAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarningTriggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarningTriggers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarningTriggers_AutoModerationRules_AutoModerationRulesId",
                        column: x => x.AutoModerationRulesId,
                        principalTable: "AutoModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarningTriggers_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_ActionId",
                table: "AuthorizationRule",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAction_GuildId",
                table: "ModerationAction",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAction_ModeratorId_GuildId",
                table: "ModerationAction",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_ActionId",
                table: "ReprimandAction",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildEntityId",
                table: "ReprimandAction",
                column: "GuildEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_UserId_GuildId",
                table: "ReprimandAction",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarningTriggers_ActionId",
                table: "WarningTriggers",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_WarningTriggers_AutoModerationRulesId",
                table: "WarningTriggers",
                column: "AutoModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_ModerationAction_ActionId",
                table: "AuthorizationRule",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_ModerationAction_ActionId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_WarningTriggers_BanTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_WarningTriggers_KickTriggerId",
                table: "AutoModerationRules");

            migrationBuilder.DropTable(
                name: "ReprimandAction");

            migrationBuilder.DropTable(
                name: "WarningTriggers");

            migrationBuilder.DropTable(
                name: "ModerationAction");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_ActionId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "AuthorizationRule");

            migrationBuilder.CreateTable(
                name: "Ban",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ban", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ban_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ban_Users_ModeratorId_GuildId",
                        columns: x => new { x.ModeratorId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ban_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "Kick",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kick", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kick_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kick_Users_ModeratorId_GuildId",
                        columns: x => new { x.ModeratorId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kick_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
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
                name: "Mute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mute_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mute_Users_ModeratorId_GuildId",
                        columns: x => new { x.ModeratorId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mute_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MuteTrigger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "Warning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
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
                        name: "FK_Warning_Users_ModeratorId_GuildId",
                        columns: x => new { x.ModeratorId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Warning_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ban_GuildId",
                table: "Ban",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_ModeratorId_GuildId",
                table: "Ban",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ban_UserId_GuildId",
                table: "Ban",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Kick_GuildId",
                table: "Kick",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_ModeratorId_GuildId",
                table: "Kick",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Kick_UserId_GuildId",
                table: "Kick",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mute_GuildId",
                table: "Mute",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_ModeratorId_GuildId",
                table: "Mute",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mute_UserId_GuildId",
                table: "Mute",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_MuteTrigger_AutoModerationRulesId",
                table: "MuteTrigger",
                column: "AutoModerationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildId",
                table: "Warning",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_ModeratorId_GuildId",
                table: "Warning",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Warning_UserId_GuildId",
                table: "Warning",
                columns: new[] { "UserId", "GuildId" });

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
    }
}
