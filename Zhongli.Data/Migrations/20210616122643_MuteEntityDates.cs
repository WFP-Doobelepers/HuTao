using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class MuteEntityDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "End",
                table: "Mute",
                newName: "EndedAt");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "Mute",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Mute");

            migrationBuilder.RenameColumn(
                name: "EndedAt",
                table: "Mute",
                newName: "End");
        }
    }
}
