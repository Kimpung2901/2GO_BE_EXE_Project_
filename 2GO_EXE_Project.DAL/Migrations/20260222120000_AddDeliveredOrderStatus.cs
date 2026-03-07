using Microsoft.EntityFrameworkCore.Migrations;

namespace _2GO_EXE_Project.DAL.Migrations;

public partial class AddDeliveredOrderStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
            ALTER TABLE "Orders" ADD CONSTRAINT "CK_Orders_Status"
            CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Delivered','Completed','Cancelled','Disputed'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
            ALTER TABLE "Orders" ADD CONSTRAINT "CK_Orders_Status"
            CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Completed','Cancelled','Disputed'));
            """);
    }
}
