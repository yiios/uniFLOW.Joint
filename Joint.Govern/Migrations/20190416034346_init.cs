using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Joint.Govern.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "LicenseKeys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KeyCode = table.Column<string>(nullable: false),
                    Module = table.Column<string>(nullable: false),
                    ModuleInstanceId = table.Column<int>(nullable: true),
                    Capacity = table.Column<int>(nullable: true),
                    IssueDate = table.Column<DateTime>(nullable: true),
                    ExpireDate = table.Column<DateTime>(nullable: true),
                    Remarks = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleInstanceId = table.Column<int>(nullable: false),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleInstances",
                columns: table => new
                {
                    ModuleInstanceId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Module = table.Column<string>(nullable: false),
                    Version = table.Column<string>(nullable: false),
                    Connected = table.Column<bool>(nullable: false),
                    EndPoint = table.Column<string>(nullable: true),
                    LastAccessTime = table.Column<DateTime>(nullable: false),
                    ConfigTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleInstances", x => x.ModuleInstanceId);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "AdminId", "Login", "PasswordHash" },
                values: new object[] { 1, "admin", "F4E1B9EB0780D62BDB3B6193829F1721" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleConfigurations_ModuleInstanceId_Key",
                table: "ModuleConfigurations",
                columns: new[] { "ModuleInstanceId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleInstances_Name",
                table: "ModuleInstances",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "LicenseKeys");

            migrationBuilder.DropTable(
                name: "ModuleConfigurations");

            migrationBuilder.DropTable(
                name: "ModuleInstances");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
