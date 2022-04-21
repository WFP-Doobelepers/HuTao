using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class ExpirableReprimands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpireAt",
                table: "ReprimandAction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "NoticeAutoPardonLength",
                table: "Guilds",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WarningAutoPardonLength",
                table: "Guilds",
                type: "interval",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "NoticeAutoPardonLength",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WarningAutoPardonLength",
                table: "Guilds");
        }
    }
}