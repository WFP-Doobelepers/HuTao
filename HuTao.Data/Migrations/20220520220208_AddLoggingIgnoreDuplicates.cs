using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddLoggingIgnoreDuplicates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_ModerationLoggingRules_ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingRulesId",
                table: "ModerationCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_CommandLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_ModeratorLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_PublicLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_UserLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "HistoryReprimands",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "SummaryReprimands",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "HistoryReprimands",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "SummaryReprimands",
                table: "ModerationCategory");

            migrationBuilder.DropColumn(
                name: "ModerationLoggingRulesId",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "LoggingRulesId",
                table: "ModerationCategory",
                newName: "LoggingId");

            migrationBuilder.RenameIndex(
                name: "IX_ModerationCategory_LoggingRulesId",
                table: "ModerationCategory",
                newName: "IX_ModerationCategory_LoggingId");

            migrationBuilder.AddColumn<Guid>(
                name: "LoggingId",
                table: "ModerationRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "SilentReprimands",
                table: "ModerationLoggingRules",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ModeratorLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommandLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "HistoryReprimands",
                table: "ModerationLoggingRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreDuplicates",
                table: "ModerationLoggingRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReprimands",
                table: "ModerationLoggingRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ShowAppealOnReprimands",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Options",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "LogReprimands",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "LogReprimandStatus",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRules_LoggingId",
                table: "ModerationRules",
                column: "LoggingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingId",
                table: "ModerationCategory",
                column: "LoggingId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_CommandLogId",
                table: "ModerationLoggingRules",
                column: "CommandLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_ModeratorLogId",
                table: "ModerationLoggingRules",
                column: "ModeratorLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_PublicLogId",
                table: "ModerationLoggingRules",
                column: "PublicLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_UserLogId",
                table: "ModerationLoggingRules",
                column: "UserLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationRules_ModerationLoggingRules_LoggingId",
                table: "ModerationRules",
                column: "LoggingId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingId",
                table: "ModerationCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_CommandLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_ModeratorLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_PublicLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_UserLogId",
                table: "ModerationLoggingRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationRules_ModerationLoggingRules_LoggingId",
                table: "ModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_ModerationRules_LoggingId",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "LoggingId",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "HistoryReprimands",
                table: "ModerationLoggingRules");

            migrationBuilder.DropColumn(
                name: "IgnoreDuplicates",
                table: "ModerationLoggingRules");

            migrationBuilder.DropColumn(
                name: "SummaryReprimands",
                table: "ModerationLoggingRules");

            migrationBuilder.RenameColumn(
                name: "LoggingId",
                table: "ModerationCategory",
                newName: "LoggingRulesId");

            migrationBuilder.RenameIndex(
                name: "IX_ModerationCategory_LoggingId",
                table: "ModerationCategory",
                newName: "IX_ModerationCategory_LoggingRulesId");

            migrationBuilder.AddColumn<int>(
                name: "HistoryReprimands",
                table: "ModerationRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReprimands",
                table: "ModerationRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SilentReprimands",
                table: "ModerationLoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ModeratorLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CommandLogId",
                table: "ModerationLoggingRules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ShowAppealOnReprimands",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Options",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LogReprimands",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LogReprimandStatus",
                table: "ModerationLogConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoryReprimands",
                table: "ModerationCategory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReprimands",
                table: "ModerationCategory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationLoggingRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_ModerationLoggingRulesId",
                table: "Guilds",
                column: "ModerationLoggingRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_ModerationLoggingRules_ModerationLoggingRulesId",
                table: "Guilds",
                column: "ModerationLoggingRulesId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationCategory_ModerationLoggingRules_LoggingRulesId",
                table: "ModerationCategory",
                column: "LoggingRulesId",
                principalTable: "ModerationLoggingRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_CommandLogId",
                table: "ModerationLoggingRules",
                column: "CommandLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_ModeratorLogId",
                table: "ModerationLoggingRules",
                column: "ModeratorLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_PublicLogId",
                table: "ModerationLoggingRules",
                column: "PublicLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationLoggingRules_ModerationLogConfig_UserLogId",
                table: "ModerationLoggingRules",
                column: "UserLogId",
                principalTable: "ModerationLogConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
