using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class CriteriaAbstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationRule");

            migrationBuilder.CreateTable(
                name: "Criterion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CensorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    IsCategory = table.Column<bool>(type: "boolean", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Permission = table.Column<int>(type: "integer", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criterion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Criterion_AuthorizationGroup_AuthorizationGroupId",
                        column: x => x.AuthorizationGroupId,
                        principalTable: "AuthorizationGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Criterion_Censor_CensorId",
                        column: x => x.CensorId,
                        principalTable: "Censor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Criterion_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_ActionId",
                table: "Criterion",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_AuthorizationGroupId",
                table: "Criterion",
                column: "AuthorizationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_CensorId",
                table: "Criterion",
                column: "CensorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Criterion");

            migrationBuilder.CreateTable(
                name: "AuthorizationRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    IsCategory = table.Column<bool>(type: "boolean", nullable: true),
                    Permission = table.Column<int>(type: "integer", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationRule_AuthorizationGroup_AuthorizationGroupId",
                        column: x => x.AuthorizationGroupId,
                        principalTable: "AuthorizationGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorizationRule_ModerationAction_ActionId",
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
                name: "IX_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule",
                column: "AuthorizationGroupId");
        }
    }
}
