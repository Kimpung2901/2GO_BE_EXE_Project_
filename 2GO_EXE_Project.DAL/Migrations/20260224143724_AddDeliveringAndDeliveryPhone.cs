using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveringAndDeliveryPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhone",
                table: "Orders",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
                ALTER TABLE "Orders" ADD CONSTRAINT "CK_Orders_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Delivering','Delivered','Completed','Cancelled','Disputed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";
                ALTER TABLE "Orders" ADD CONSTRAINT "CK_Orders_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Delivered','Completed','Cancelled','Disputed'));
                """);

            migrationBuilder.DropColumn(
                name: "DeliveryPhone",
                table: "Orders");
        }
    }
}
