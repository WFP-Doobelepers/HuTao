using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddLinkedButtonDmUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DmUser",
                table: "LinkedButton",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DmUser",
                table: "LinkedButton");
        }
    }
}