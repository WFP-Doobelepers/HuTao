using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddModerationLoggingRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnumChannels_ModerationRules_ModerationRulesId",
                table: "EnumChannels");

            migrationBuilder.DropIndex(
                name: "IX_EnumChannels_ModerationRulesId",
                table: "EnumChannels");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "NotifyReprimands",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ReprimandAppealMessage",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ShowAppealOnReprimands",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ModerationRulesId",
                table: "EnumChannels");

            migrationBuilder.CreateTable(
                name: "ModerationLogConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Options = table.Column<int>(type: "integer", nullable: false),
                    LogReprimands = table.Column<int>(type: "integer", nullable: false),
                    ShowAppealOnReprimands = table.Column<int>(type: "integer", nullable: false),
                    LogReprimandStatus = table.Column<int>(type: "integer", nullable: false),
                    AppealMessage = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationLogConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModerationLoggingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeratorLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    SilentReprimands = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationLoggingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationLoggingRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationLoggingRules_ModerationLogConfig_CommandLogId",
                        column: x => x.CommandLogId,
                        principalTable: "ModerationLogConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationLoggingRules_ModerationLogConfig_ModeratorLogId",
                        column: x => x.ModeratorLogId,
                        principalTable: "ModerationLogConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationLoggingRules_ModerationLogConfig_PublicLogId",
                        column: x => x.PublicLogId,
                        principalTable: "ModerationLogConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationLoggingRules_ModerationLogConfig_UserLogId",
                        column: x => x.UserLogId,
                        principalTable: "ModerationLogConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_CommandLogId",
                table: "ModerationLoggingRules",
                column: "CommandLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_GuildId",
                table: "ModerationLoggingRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_ModeratorLogId",
                table: "ModerationLoggingRules",
                column: "ModeratorLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_PublicLogId",
                table: "ModerationLoggingRules",
                column: "PublicLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationLoggingRules_UserLogId",
                table: "ModerationLoggingRules",
                column: "UserLogId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationLoggingRules");

            migrationBuilder.DropTable(
                name: "ModerationLogConfig");

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "ModerationRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotifyReprimands",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReprimandAppealMessage",
                table: "LoggingRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShowAppealOnReprimands",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationRulesId",
                table: "EnumChannels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnumChannels_ModerationRulesId",
                table: "EnumChannels",
                column: "ModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnumChannels_ModerationRules_ModerationRulesId",
                table: "EnumChannels",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id");
        }
    }
}