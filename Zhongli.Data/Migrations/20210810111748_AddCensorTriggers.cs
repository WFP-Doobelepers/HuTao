using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddCensorTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Amount",
                table: "Censor",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "Censor",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Censor");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Censor");
        }
    }
}
