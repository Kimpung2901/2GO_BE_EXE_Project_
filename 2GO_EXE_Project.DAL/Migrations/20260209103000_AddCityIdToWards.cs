using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260209103000_AddCityIdToWards")]
public partial class AddCityIdToWards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CityId",
            table: "Wards",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Wards_CityId",
            table: "Wards",
            column: "CityId");

        migrationBuilder.AddForeignKey(
            name: "FK_Wards_Cities",
            table: "Wards",
            column: "CityId",
            principalTable: "Cities",
            principalColumn: "CityId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Wards_Cities",
            table: "Wards");

        migrationBuilder.DropIndex(
            name: "IX_Wards_CityId",
            table: "Wards");

        migrationBuilder.DropColumn(
            name: "CityId",
            table: "Wards");
    }
}
