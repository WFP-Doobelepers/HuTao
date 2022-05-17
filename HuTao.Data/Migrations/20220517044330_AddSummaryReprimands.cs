using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddSummaryReprimands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SummaryReprimands",
                table: "ModerationRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReprimands",
                table: "ModerationCategory",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummaryReprimands",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "SummaryReprimands",
                table: "ModerationCategory");
        }
    }
}
