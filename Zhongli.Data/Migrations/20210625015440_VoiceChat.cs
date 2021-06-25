using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class VoiceChat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceChatRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurgeEmpty = table.Column<bool>(type: "boolean", nullable: false),
                    ShowJoinLeave = table.Column<bool>(type: "boolean", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    HubVoiceChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VoiceChannelCategoryId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VoiceChatCategoryId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceChatRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceChatRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceChatLink",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OwnerId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TextChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VoiceChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VoiceChatRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceChatLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceChatLink_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoiceChatLink_Users_OwnerId_GuildId",
                        columns: x => new { x.OwnerId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoiceChatLink_VoiceChatRules_VoiceChatRulesId",
                        column: x => x.VoiceChatRulesId,
                        principalTable: "VoiceChatRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChatLink_GuildId",
                table: "VoiceChatLink",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChatLink_OwnerId_GuildId",
                table: "VoiceChatLink",
                columns: new[] { "OwnerId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChatLink_VoiceChatRulesId",
                table: "VoiceChatLink",
                column: "VoiceChatRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChatRules_GuildId",
                table: "VoiceChatRules",
                column: "GuildId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceChatLink");

            migrationBuilder.DropTable(
                name: "VoiceChatRules");
        }
    }
}
