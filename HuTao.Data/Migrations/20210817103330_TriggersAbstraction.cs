using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class TriggersAbstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_Censor_CensorId",
                table: "Criterion");

            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Censor_CensorId",
                table: "ReprimandAction");

            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ActionId",
                table: "ReprimandAction");

            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Users_UserId_GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropTable(
                name: "Censor");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_ActionId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_CensorId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_UserId_GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "DeleteDays",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "CensorId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ReprimandAction");

            migrationBuilder.AlterColumn<int>(
                name: "Source",
                table: "Trigger",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "Trigger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pattern",
                table: "Trigger",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReprimandId",
                table: "Trigger",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reprimand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Count = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reprimand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reprimand_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reprimand_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reprimand_ModerationAction_ModifiedActionId",
                        column: x => x.ModifiedActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reprimand_Trigger_TriggerId",
                        column: x => x.TriggerId,
                        principalTable: "Trigger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reprimand_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_ReprimandId",
                table: "Trigger",
                column: "ReprimandId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_ActionId",
                table: "Reprimand",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_GuildId",
                table: "Reprimand",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_ModifiedActionId",
                table: "Reprimand",
                column: "ModifiedActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_TriggerId",
                table: "Reprimand",
                column: "TriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_UserId_GuildId",
                table: "Reprimand",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_Trigger_CensorId",
                table: "Criterion",
                column: "CensorId",
                principalTable: "Trigger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ReprimandAction_ReprimandId",
                table: "Trigger",
                column: "ReprimandId",
                principalTable: "ReprimandAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_Trigger_CensorId",
                table: "Criterion");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ReprimandAction_ReprimandId",
                table: "Trigger");

            migrationBuilder.DropTable(
                name: "Reprimand");

            migrationBuilder.DropIndex(
                name: "IX_Trigger_ReprimandId",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "ReprimandId",
                table: "Trigger");

            migrationBuilder.AlterColumn<int>(
                name: "Source",
                table: "Trigger",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Count",
                table: "Trigger",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeleteDays",
                table: "Trigger",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Length",
                table: "Trigger",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CensorId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ReprimandAction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "ReprimandAction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpireAt",
                table: "ReprimandAction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "ReprimandAction",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedActionId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ReprimandAction",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "ReprimandAction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ReprimandAction",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UserId",
                table: "ReprimandAction",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Censor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    ModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Options = table.Column<int>(type: "integer", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: false),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Count = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Censor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Censor_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Censor_ModerationRules_ModerationRulesId",
                        column: x => x.ModerationRulesId,
                        principalTable: "ModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_ActionId",
                table: "ReprimandAction",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_CensorId",
                table: "ReprimandAction",
                column: "CensorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildId",
                table: "ReprimandAction",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_ModifiedActionId",
                table: "ReprimandAction",
                column: "ModifiedActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_UserId_GuildId",
                table: "ReprimandAction",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Censor_ActionId",
                table: "Censor",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Censor_ModerationRulesId",
                table: "Censor",
                column: "ModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_Censor_CensorId",
                table: "Criterion",
                column: "CensorId",
                principalTable: "Censor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Censor_CensorId",
                table: "ReprimandAction",
                column: "CensorId",
                principalTable: "Censor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildId",
                table: "ReprimandAction",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ActionId",
                table: "ReprimandAction",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ModifiedActionId",
                table: "ReprimandAction",
                column: "ModifiedActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Users_UserId_GuildId",
                table: "ReprimandAction",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}