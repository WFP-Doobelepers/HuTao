using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddTimeTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GenshinRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimeTrackingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeTrackingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenshinTimeTrackingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmericaChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    EuropeChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    AsiaChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    SARChannelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenshinTimeTrackingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenshinTimeTrackingRules_TimeTrackingRules_AmericaChannelId",
                        column: x => x.AmericaChannelId,
                        principalTable: "TimeTrackingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenshinTimeTrackingRules_TimeTrackingRules_AsiaChannelId",
                        column: x => x.AsiaChannelId,
                        principalTable: "TimeTrackingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenshinTimeTrackingRules_TimeTrackingRules_EuropeChannelId",
                        column: x => x.EuropeChannelId,
                        principalTable: "TimeTrackingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenshinTimeTrackingRules_TimeTrackingRules_SARChannelId",
                        column: x => x.SARChannelId,
                        principalTable: "TimeTrackingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GenshinTimeTrackingRules_TimeTrackingRules_ServerStatusId",
                        column: x => x.ServerStatusId,
                        principalTable: "TimeTrackingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_GenshinRulesId",
                table: "Guilds",
                column: "GenshinRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_GenshinTimeTrackingRules_AmericaChannelId",
                table: "GenshinTimeTrackingRules",
                column: "AmericaChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_GenshinTimeTrackingRules_AsiaChannelId",
                table: "GenshinTimeTrackingRules",
                column: "AsiaChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_GenshinTimeTrackingRules_EuropeChannelId",
                table: "GenshinTimeTrackingRules",
                column: "EuropeChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_GenshinTimeTrackingRules_SARChannelId",
                table: "GenshinTimeTrackingRules",
                column: "SARChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_GenshinTimeTrackingRules_ServerStatusId",
                table: "GenshinTimeTrackingRules",
                column: "ServerStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_GenshinTimeTrackingRules_GenshinRulesId",
                table: "Guilds",
                column: "GenshinRulesId",
                principalTable: "GenshinTimeTrackingRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_GenshinTimeTrackingRules_GenshinRulesId",
                table: "Guilds");

            migrationBuilder.DropTable(
                name: "GenshinTimeTrackingRules");

            migrationBuilder.DropTable(
                name: "TimeTrackingRules");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_GenshinRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GenshinRulesId",
                table: "Guilds");
        }
    }
}
