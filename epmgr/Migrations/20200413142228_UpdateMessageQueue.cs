using Microsoft.EntityFrameworkCore.Migrations;

namespace epmgr.Migrations
{
    public partial class UpdateMessageQueue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "MessageQueue",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "MessageQueue");
        }
    }
}
