using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class AddModerationTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModerationTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Count = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationTemplate_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationTemplate_GuildEntityId",
                table: "ModerationTemplate",
                column: "GuildEntityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationTemplate");
        }
    }
}
