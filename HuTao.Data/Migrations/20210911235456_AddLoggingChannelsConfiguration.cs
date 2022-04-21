using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddLoggingChannelsConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageLogChannelId",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ModerationChannelId",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "LoggingRules");

            migrationBuilder.DropColumn(
                name: "ReactionLogChannelId",
                table: "LoggingRules");

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "ModerationRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EnumChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntType = table.Column<int>(type: "integer", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    LoggingRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnumChannels_LoggingRules_LoggingRulesId",
                        column: x => x.LoggingRulesId,
                        principalTable: "LoggingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnumChannels_ModerationRules_ModerationRulesId",
                        column: x => x.ModerationRulesId,
                        principalTable: "ModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnumChannels_LoggingRulesId",
                table: "EnumChannels",
                column: "LoggingRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_EnumChannels_ModerationRulesId",
                table: "EnumChannels",
                column: "ModerationRulesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnumChannels");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "ModerationRules");

            migrationBuilder.AddColumn<decimal>(
                name: "MessageLogChannelId",
                table: "LoggingRules",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ModerationChannelId",
                table: "LoggingRules",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReactionLogChannelId",
                table: "LoggingRules",
                type: "numeric(20,0)",
                nullable: true);
        }
    }
}