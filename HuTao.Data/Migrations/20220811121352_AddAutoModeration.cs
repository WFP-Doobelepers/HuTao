using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddAutoModeration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModerationRules_AntiSpamRules_AntiSpamRulesId",
                table: "ModerationRules");

            migrationBuilder.DropTable(
                name: "AntiSpamRules");

            migrationBuilder.DropIndex(
                name: "IX_ModerationRules_AntiSpamRulesId",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "AntiSpamRulesId",
                table: "ModerationRules");

            migrationBuilder.RenameColumn(
                name: "CensorTimeRange",
                table: "ModerationRules",
                newName: "FilteredExpiryLength");

            migrationBuilder.RenameColumn(
                name: "CensorTimeRange",
                table: "ModerationCategory",
                newName: "FilteredExpiryLength");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Trigger",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "BlankOnly",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Cooldown",
                table: "Trigger",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountDuplicate",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountInvalid",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountRoleMembers",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeleteMessages",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Global",
                table: "Trigger",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Length",
                table: "Trigger",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumLength",
                table: "Trigger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Percentage",
                table: "Trigger",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Trigger",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tolerance",
                table: "Trigger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Trigger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AutoReprimandCooldown",
                table: "ModerationRules",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CensorNicknames",
                table: "ModerationRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CensorUsernames",
                table: "ModerationRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CensoredExpiryLength",
                table: "ModerationRules",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameReplacement",
                table: "ModerationRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AutoReprimandCooldown",
                table: "ModerationCategory",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CensorNicknames",
                table: "ModerationCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CensorUsernames",
                table: "ModerationCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CensoredExpiryLength",
                table: "ModerationCategory",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameReplacement",
                table: "ModerationCategory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FilteredId",
                table: "DeleteLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Link",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalString = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Link", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModerationExclusion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmojiId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationExclusion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Criterion_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criterion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Link_LinkId",
                        column: x => x.LinkId,
                        principalTable: "Link",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_ModerationRules_ModerationRulesId",
                        column: x => x.ModerationRulesId,
                        principalTable: "ModerationRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_ReactionEntity_EmojiId",
                        column: x => x.EmojiId,
                        principalTable: "ReactionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Trigger_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "Trigger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModerationExclusion_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeleteLog_FilteredId",
                table: "DeleteLog",
                column: "FilteredId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_ConfigurationId",
                table: "ModerationExclusion",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_CriterionId",
                table: "ModerationExclusion",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_EmojiId",
                table: "ModerationExclusion",
                column: "EmojiId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_GuildId",
                table: "ModerationExclusion",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_LinkId",
                table: "ModerationExclusion",
                column: "LinkId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_ModerationRulesId",
                table: "ModerationExclusion",
                column: "ModerationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_RoleId",
                table: "ModerationExclusion",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationExclusion_UserId_GuildId",
                table: "ModerationExclusion",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeleteLog_Reprimand_FilteredId",
                table: "DeleteLog",
                column: "FilteredId",
                principalTable: "Reprimand",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeleteLog_Reprimand_FilteredId",
                table: "DeleteLog");

            migrationBuilder.DropTable(
                name: "ModerationExclusion");

            migrationBuilder.DropTable(
                name: "Link");

            migrationBuilder.DropIndex(
                name: "IX_DeleteLog_FilteredId",
                table: "DeleteLog");

            migrationBuilder.DropColumn(
                name: "BlankOnly",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Cooldown",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "CountDuplicate",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "CountInvalid",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "CountRoleMembers",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "DeleteMessages",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Global",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "MinimumLength",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Tolerance",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "AutoReprimandCooldown",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "CensorNicknames",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "CensorUsernames",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "CensoredExpiryLength",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "NameReplacement",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "AutoReprimandCooldown",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "CensorNicknames",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "CensorUsernames",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "CensoredExpiryLength",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "NameReplacement",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "FilteredId",
                table: "DeleteLog");

            migrationBuilder.RenameColumn(
                name: "FilteredExpiryLength",
                table: "ModerationRules",
                newName: "CensorTimeRange");

            migrationBuilder.RenameColumn(
                name: "FilteredExpiryLength",
                table: "ModerationCategory",
                newName: "CensorTimeRange");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Trigger",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<Guid>(
                name: "AntiSpamRulesId",
                table: "ModerationRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AntiSpamRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DuplicateMessageTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    DuplicateTolerance = table.Column<int>(type: "integer", nullable: true),
                    EmojiLimit = table.Column<long>(type: "bigint", nullable: true),
                    MessageLimit = table.Column<long>(type: "bigint", nullable: true),
                    MessageSpamTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    NewlineLimit = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRules_AntiSpamRulesId",
                table: "ModerationRules",
                column: "AntiSpamRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamRules_GuildId",
                table: "AntiSpamRules",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationRules_AntiSpamRules_AntiSpamRulesId",
                table: "ModerationRules",
                column: "AntiSpamRulesId",
                principalTable: "AntiSpamRules",
                principalColumn: "Id");
        }
    }
}
