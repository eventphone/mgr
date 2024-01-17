using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using epmgr.Data;

namespace epmgr.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:mgr_extension_type", "dect,sip,premium,group,special,gsm,app,announcement");

            migrationBuilder.CreateTable(
                name: "DeviceQueue",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<long>(nullable: false),
                    Extension = table.Column<string>(maxLength: 32, nullable: true),
                    Ipei = table.Column<string>(maxLength: 15, nullable: true),
                    Uak = table.Column<string>(maxLength: 32, nullable: true),
                    Failed = table.Column<bool>(nullable: false),
                    Error = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Extension",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Extension = table.Column<string>(maxLength: 32, nullable: true),
                    Type = table.Column<MgrExtensionType>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: true),
                    Password = table.Column<string>(maxLength: 32, nullable: true),
                    Token = table.Column<string>(maxLength: 32, nullable: true),
                    Language = table.Column<string>(maxLength: 5, nullable: true),
                    UseEncryption = table.Column<bool>(nullable: false),
                    DeleteAfterResync = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Extension", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Printer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: true),
                    IpAddress = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Printer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(maxLength: 64, nullable: true),
                    PasswordHash = table.Column<string>(maxLength: 255, nullable: true),
                    LastLogon = table.Column<DateTimeOffset>(nullable: false),
                    PrinterId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Printer_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "LastLogon", "PasswordHash", "PrinterId", "Username" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AQAAAAEAACcQAAAAEB8YUrE2oHquTwLo4LGsvEmN10u7KUDg/AMreQ2sQLqfA2ln0hCaic1Grcc/qQ5+ew==", null, "mgr" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceQueue_Failed",
                table: "DeviceQueue",
                column: "Failed");

            migrationBuilder.CreateIndex(
                name: "IX_Extension_Extension",
                table: "Extension",
                column: "Extension",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_PrinterId",
                table: "User",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "User",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceQueue");

            migrationBuilder.DropTable(
                name: "Extension");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Printer");
        }
    }
}
