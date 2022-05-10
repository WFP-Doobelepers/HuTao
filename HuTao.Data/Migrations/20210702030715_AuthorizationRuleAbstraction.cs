using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AuthorizationRuleAbstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_AuthorizationRules_AuthorizationRulesId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Guilds_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_UserId_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropTable(
                name: "ChannelAuthorization");

            migrationBuilder.DropTable(
                name: "GuildAuthorization");

            migrationBuilder.DropTable(
                name: "PermissionAuthorization");

            migrationBuilder.DropTable(
                name: "RoleAuthorization");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAuthorization",
                table: "UserAuthorization");

            migrationBuilder.RenameTable(
                name: "UserAuthorization",
                newName: "AuthorizationRule");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorization_UserId_GuildId",
                table: "AuthorizationRule",
                newName: "IX_AuthorizationRule_UserId_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorization_GuildId",
                table: "AuthorizationRule",
                newName: "IX_AuthorizationRule_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorization_AuthorizationRulesId",
                table: "AuthorizationRule",
                newName: "IX_AuthorizationRule_AuthorizationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorization_AddedById_GuildId",
                table: "AuthorizationRule",
                newName: "IX_AuthorizationRule_AddedById_GuildId");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "AuthorizationRule",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorizationRulesId",
                table: "AuthorizationRule",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChannelId",
                table: "AuthorizationRule",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AuthorizationRule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Permission",
                table: "AuthorizationRule",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RoleId",
                table: "AuthorizationRule",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuthorizationRule",
                table: "AuthorizationRule",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRules_AuthorizationRulesId",
                table: "AuthorizationRule",
                column: "AuthorizationRulesId",
                principalTable: "AuthorizationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Guilds_GuildId",
                table: "AuthorizationRule",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Users_AddedById_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "AddedById", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Users_UserId_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRules_AuthorizationRulesId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Guilds_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Users_AddedById_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Users_UserId_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthorizationRule",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "Permission",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AuthorizationRule");

            migrationBuilder.RenameTable(
                name: "AuthorizationRule",
                newName: "UserAuthorization");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorizationRule_UserId_GuildId",
                table: "UserAuthorization",
                newName: "IX_UserAuthorization_UserId_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorizationRule_GuildId",
                table: "UserAuthorization",
                newName: "IX_UserAuthorization_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorizationRule_AuthorizationRulesId",
                table: "UserAuthorization",
                newName: "IX_UserAuthorization_AuthorizationRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorizationRule_AddedById_GuildId",
                table: "UserAuthorization",
                newName: "IX_UserAuthorization_AddedById_GuildId");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "UserAuthorization",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorizationRulesId",
                table: "UserAuthorization",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAuthorization",
                table: "UserAuthorization",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ChannelAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_ChannelAuthorization_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelAuthorization_Users_AddedById_AddedByGuildId",
                        columns: x => new { x.AddedById, x.AddedByGuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_GuildAuthorization_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildAuthorization_Users_AddedById_AddedByGuildId",
                        columns: x => new { x.AddedById, x.AddedByGuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_PermissionAuthorization_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionAuthorization_Users_AddedById_AddedByGuildId",
                        columns: x => new { x.AddedById, x.AddedByGuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleAuthorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AuthorizationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false)
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
                        name: "FK_RoleAuthorization_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleAuthorization_Users_AddedById_AddedByGuildId",
                        columns: x => new { x.AddedById, x.AddedByGuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AddedById_AddedByGuildId",
                table: "ChannelAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AuthorizationRulesId",
                table: "ChannelAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_GuildId",
                table: "ChannelAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AddedById_AddedByGuildId",
                table: "GuildAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AuthorizationRulesId",
                table: "GuildAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_GuildId",
                table: "GuildAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AddedById_AddedByGuildId",
                table: "PermissionAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AuthorizationRulesId",
                table: "PermissionAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_GuildId",
                table: "PermissionAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AddedById_AddedByGuildId",
                table: "RoleAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AuthorizationRulesId",
                table: "RoleAuthorization",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_GuildId",
                table: "RoleAuthorization",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_AuthorizationRules_AuthorizationRulesId",
                table: "UserAuthorization",
                column: "AuthorizationRulesId",
                principalTable: "AuthorizationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Guilds_GuildId",
                table: "UserAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_GuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_UserId_GuildId",
                table: "UserAuthorization",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}