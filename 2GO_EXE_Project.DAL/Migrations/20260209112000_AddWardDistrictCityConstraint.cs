using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260209112000_AddWardDistrictCityConstraint")]
public partial class AddWardDistrictCityConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Wards_Districts",
            table: "Wards");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_Districts_DistrictId_CityId",
            table: "Districts",
            columns: new[] { "DistrictId", "CityId" });

        migrationBuilder.CreateIndex(
            name: "IX_Wards_DistrictId_CityId",
            table: "Wards",
            columns: new[] { "DistrictId", "CityId" });

        migrationBuilder.AddForeignKey(
            name: "FK_Wards_Districts_City",
            table: "Wards",
            columns: new[] { "DistrictId", "CityId" },
            principalTable: "Districts",
            principalColumns: new[] { "DistrictId", "CityId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Wards_Districts_City",
            table: "Wards");

        migrationBuilder.DropIndex(
            name: "IX_Wards_DistrictId_CityId",
            table: "Wards");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_Districts_DistrictId_CityId",
            table: "Districts");

        migrationBuilder.AddForeignKey(
            name: "FK_Wards_Districts",
            table: "Wards",
            column: "DistrictId",
            principalTable: "Districts",
            principalColumn: "DistrictId");
    }
}
