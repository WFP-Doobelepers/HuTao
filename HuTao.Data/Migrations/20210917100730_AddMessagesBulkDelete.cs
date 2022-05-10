using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddMessagesBulkDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MessagesDeleteLogId",
                table: "DeleteLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeleteLog_MessagesDeleteLogId",
                table: "DeleteLog",
                column: "MessagesDeleteLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeleteLog_DeleteLog_MessagesDeleteLogId",
                table: "DeleteLog",
                column: "MessagesDeleteLogId",
                principalTable: "DeleteLog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeleteLog_DeleteLog_MessagesDeleteLogId",
                table: "DeleteLog");

            migrationBuilder.DropIndex(
                name: "IX_DeleteLog_MessagesDeleteLogId",
                table: "DeleteLog");

            migrationBuilder.DropColumn(
                name: "MessagesDeleteLogId",
                table: "DeleteLog");
        }
    }
}