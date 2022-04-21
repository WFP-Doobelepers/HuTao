using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class Net60MigrationBreakingChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_DeleteLog_ReactionEntity_EmoteId",
                table: "DeleteLog");

            migrationBuilder.DropForeignKey(
                name: "FK_ReactionLog_ReactionEntity_EmoteId",
                table: "ReactionLog");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryRole_ModerationAction_ActionId",
                table: "TemporaryRole");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "Trigger",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "TemporaryRole",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmoteId",
                table: "ReactionLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ModerationAction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeleteLog_ReactionEntity_EmoteId",
                table: "DeleteLog",
                column: "EmoteId",
                principalTable: "ReactionEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionLog_ReactionEntity_EmoteId",
                table: "ReactionLog",
                column: "EmoteId",
                principalTable: "ReactionEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryRole_ModerationAction_ActionId",
                table: "TemporaryRole",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_DeleteLog_ReactionEntity_EmoteId",
                table: "DeleteLog");

            migrationBuilder.DropForeignKey(
                name: "FK_ReactionLog_ReactionEntity_EmoteId",
                table: "ReactionLog");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryRole_ModerationAction_ActionId",
                table: "TemporaryRole");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "Trigger",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "TemporaryRole",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmoteId",
                table: "ReactionLog",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ModerationAction",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeleteLog_ReactionEntity_EmoteId",
                table: "DeleteLog",
                column: "EmoteId",
                principalTable: "ReactionEntity",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionLog_ReactionEntity_EmoteId",
                table: "ReactionLog",
                column: "EmoteId",
                principalTable: "ReactionEntity",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryRole_ModerationAction_ActionId",
                table: "TemporaryRole",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id");
        }
    }
}