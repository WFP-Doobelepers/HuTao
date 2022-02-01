using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class AddLinkedButtonNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LinkedButton_ButtonId",
                table: "LinkedButton");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedButton_ButtonId",
                table: "LinkedButton",
                column: "ButtonId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LinkedButton_ButtonId",
                table: "LinkedButton");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedButton_ButtonId",
                table: "LinkedButton",
                column: "ButtonId");
        }
    }
}
