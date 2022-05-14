using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddHistoryReprimands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HistoryReprimands",
                table: "ModerationRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoryReprimands",
                table: "ModerationCategory",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HistoryReprimands",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "HistoryReprimands",
                table: "ModerationCategory");
        }
    }
}
