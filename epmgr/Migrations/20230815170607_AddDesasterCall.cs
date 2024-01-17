using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace epmgr.Migrations
{
    public partial class AddDesasterCall : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesasterCall",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Pin = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    Announcement = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Target = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesasterCall", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesasterCall_Pin",
                table: "DesasterCall",
                column: "Pin",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesasterCall");
        }
    }
}
