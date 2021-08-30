using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddDeleteLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCategory",
                table: "ReactionLog");

            migrationBuilder.DropColumn(
                name: "LogType",
                table: "ReactionLog");

            migrationBuilder.DropColumn(
                name: "LogType",
                table: "MessageLog");

            migrationBuilder.CreateTable(
                name: "DeleteLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    EmoteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeleteLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeleteLog_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeleteLog_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeleteLog_ReactionEntity_EmoteId",
                        column: x => x.EmoteId,
                        principalTable: "ReactionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeleteLog_ActionId",
                table: "DeleteLog",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeleteLog_EmoteId",
                table: "DeleteLog",
                column: "EmoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeleteLog_GuildEntityId",
                table: "DeleteLog",
                column: "GuildEntityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeleteLog");

            migrationBuilder.AddColumn<bool>(
                name: "IsCategory",
                table: "ReactionLog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LogType",
                table: "ReactionLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LogType",
                table: "MessageLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
