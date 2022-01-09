using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class CompleteEmbedEntityModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup");

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

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "Embed",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FooterId",
                table: "Embed",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "Embed",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "Embed",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VideoId",
                table: "Embed",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActionId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "Author",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProxyIconUrl = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Author", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Field",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Inline = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    EmbedId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Field", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Field_Embed_EmbedId",
                        column: x => x.EmbedId,
                        principalTable: "Embed",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Footer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    ProxyUrl = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Footer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Image",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    ProxyUrl = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Image", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Video",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Video", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Embed_AuthorId",
                table: "Embed",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Embed_FooterId",
                table: "Embed",
                column: "FooterId");

            migrationBuilder.CreateIndex(
                name: "IX_Embed_ImageId",
                table: "Embed",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Embed_VideoId",
                table: "Embed",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Field_EmbedId",
                table: "Field",
                column: "EmbedId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embed_Author_AuthorId",
                table: "Embed",
                column: "AuthorId",
                principalTable: "Author",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embed_Footer_FooterId",
                table: "Embed",
                column: "FooterId",
                principalTable: "Footer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embed_Image_ImageId",
                table: "Embed",
                column: "ImageId",
                principalTable: "Image",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embed_Video_VideoId",
                table: "Embed",
                column: "VideoId",
                principalTable: "Video",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_Embed_Author_AuthorId",
                table: "Embed");

            migrationBuilder.DropForeignKey(
                name: "FK_Embed_Footer_FooterId",
                table: "Embed");

            migrationBuilder.DropForeignKey(
                name: "FK_Embed_Image_ImageId",
                table: "Embed");

            migrationBuilder.DropForeignKey(
                name: "FK_Embed_Video_VideoId",
                table: "Embed");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryRole_ModerationAction_ActionId",
                table: "TemporaryRole");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationAction_ActionId",
                table: "Trigger");

            migrationBuilder.DropTable(
                name: "Author");

            migrationBuilder.DropTable(
                name: "Field");

            migrationBuilder.DropTable(
                name: "Footer");

            migrationBuilder.DropTable(
                name: "Image");

            migrationBuilder.DropTable(
                name: "Video");

            migrationBuilder.DropIndex(
                name: "IX_Embed_AuthorId",
                table: "Embed");

            migrationBuilder.DropIndex(
                name: "IX_Embed_FooterId",
                table: "Embed");

            migrationBuilder.DropIndex(
                name: "IX_Embed_ImageId",
                table: "Embed");

            migrationBuilder.DropIndex(
                name: "IX_Embed_VideoId",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "FooterId",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "Embed");

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
    }
}
