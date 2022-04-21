using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AuthorizationEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthorizationRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthorizationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelAuthorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelAuthorization_AuthorizationRules_AuthorizationRulesId",
                        column: x => x.AuthorizationRulesId,
                        principalTable: "AuthorizationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChannelAuthorization_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildAuthorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildAuthorization_AuthorizationRules_AuthorizationRulesId",
                        column: x => x.AuthorizationRulesId,
                        principalTable: "AuthorizationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildAuthorization_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAuthorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionAuthorization_AuthorizationRules_AuthorizationRul~",
                        column: x => x.AuthorizationRulesId,
                        principalTable: "AuthorizationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionAuthorization_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAuthorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAuthorization_AuthorizationRules_AuthorizationRulesId",
                        column: x => x.AuthorizationRulesId,
                        principalTable: "AuthorizationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAuthorization_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthorization_AuthorizationRules_AuthorizationRulesId",
                        column: x => x.AuthorizationRulesId,
                        principalTable: "AuthorizationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuthorization_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_AuthorizationRulesId",
                table: "Guilds",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AddedById",
                table: "ChannelAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AuthorizationRulesId",
                table: "ChannelAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AddedById",
                table: "GuildAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AuthorizationRulesId",
                table: "GuildAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AddedById",
                table: "PermissionAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AuthorizationRulesId",
                table: "PermissionAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AddedById",
                table: "RoleAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AuthorizationRulesId",
                table: "RoleAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AddedById",
                table: "UserAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AuthorizationRulesId",
                table: "UserAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AuthorizationRules_AuthorizationRulesId",
                table: "Guilds",
                column: "AuthorizationRulesId",
                principalTable: "AuthorizationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AuthorizationRules_AuthorizationRulesId",
                table: "Guilds");

            migrationBuilder.DropTable(
                name: "ChannelAuthorization");

            migrationBuilder.DropTable(
                name: "GuildAuthorization");

            migrationBuilder.DropTable(
                name: "PermissionAuthorization");

            migrationBuilder.DropTable(
                name: "RoleAuthorization");

            migrationBuilder.DropTable(
                name: "UserAuthorization");

            migrationBuilder.DropTable(
                name: "AuthorizationRules");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_AuthorizationRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "AuthorizationRulesId",
                table: "Guilds");
        }
    }
}