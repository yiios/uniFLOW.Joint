using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Joint.Govern.Migrations
{
    public partial class first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleInstanceId = table.Column<int>(nullable: false),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
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
                    Connected = table.Column<bool>(nullable: false),
                    Endpoint = table.Column<string>(nullable: true),
                    LastAccessTime = table.Column<DateTime>(nullable: false),
                    ConfigTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleInstances", x => x.ModuleInstanceId);
                });

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
                name: "ModuleConfigurations");

            migrationBuilder.DropTable(
                name: "ModuleInstances");
        }
    }
}
