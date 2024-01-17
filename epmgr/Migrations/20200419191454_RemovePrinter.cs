using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace epmgr.Migrations
{
    public partial class RemovePrinter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Printer_PrinterId",
                table: "User");

            migrationBuilder.DropTable(
                name: "Printer");

            migrationBuilder.DropIndex(
                name: "IX_User_PrinterId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PrinterId",
                table: "User");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrinterId",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Printer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IpAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Printer", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_PrinterId",
                table: "User",
                column: "PrinterId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Printer_PrinterId",
                table: "User",
                column: "PrinterId",
                principalTable: "Printer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
