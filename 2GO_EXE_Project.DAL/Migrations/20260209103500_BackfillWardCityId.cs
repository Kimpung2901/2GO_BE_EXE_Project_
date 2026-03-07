using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260209103500_BackfillWardCityId")]
public partial class BackfillWardCityId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""Wards"" w
SET ""CityId"" = d.""CityId""
FROM ""Districts"" d
WHERE w.""DistrictId"" = d.""DistrictId""
  AND (w.""CityId"" IS NULL OR w.""CityId"" <> d.""CityId"");
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE ""Wards""
SET ""CityId"" = NULL;
");
    }
}
