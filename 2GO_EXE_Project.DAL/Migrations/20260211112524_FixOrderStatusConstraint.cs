using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
                ALTER TABLE "Orders"
                ADD CONSTRAINT "CK_Orders_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Completed','Cancelled','Disputed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
                ALTER TABLE "Orders"
                ADD CONSTRAINT "CK_Orders_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Completed','Cancelled','Disputed'));
                """);
        }
    }
}
