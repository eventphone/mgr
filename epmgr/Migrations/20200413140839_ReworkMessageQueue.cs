using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace epmgr.Migrations
{
    public partial class ReworkMessageQueue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceQueue");

            migrationBuilder.CreateTable(
                name: "MessageQueue",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<long>(nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: true),
                    Failed = table.Column<bool>(nullable: false),
                    Error = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageQueue_Failed",
                table: "MessageQueue",
                column: "Failed");

            migrationBuilder.CreateIndex(
                name: "IX_MessageQueue_Timestamp",
                table: "MessageQueue",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageQueue");

            migrationBuilder.CreateTable(
                name: "DeviceQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Error = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Extension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Failed = table.Column<bool>(type: "boolean", nullable: false),
                    Ipei = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Uak = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceQueue_Failed",
                table: "DeviceQueue",
                column: "Failed");
        }
    }
}
