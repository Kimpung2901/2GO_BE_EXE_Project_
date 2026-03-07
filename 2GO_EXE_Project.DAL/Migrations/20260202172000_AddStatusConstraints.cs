using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260202172000_AddStatusConstraints")]
    public partial class AddStatusConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Listings"
                ADD CONSTRAINT "CK_Listings_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Draft','PendingReview','Active','Reserved','Sold','Rejected','Archived','Flagged','Deleted'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Listings"
                ADD CONSTRAINT "CK_Listings_ListingType"
                CHECK ("ListingType" IS NULL OR "ListingType" = 'SINGLE');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Orders"
                ADD CONSTRAINT "CK_Orders_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Confirmed','Completed','Cancelled','Disputed'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Payments"
                ADD CONSTRAINT "CK_Payments_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Paid','Failed','Cancelled'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "EscrowContracts"
                ADD CONSTRAINT "CK_EscrowContracts_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Pending','Funded','Holding','Released','Cancelled','Refunded'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ShippingRequests"
                ADD CONSTRAINT "CK_ShippingRequests_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Requested','InTransit','Delivered','Failed'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Reports"
                ADD CONSTRAINT "CK_Reports_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Open','InReview','WaitingOtherParty','Resolved','Rejected'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                ADD CONSTRAINT "CK_Users_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('Active','Banned','Deleted'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Carts"
                ADD CONSTRAINT "CK_Carts_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('ACTIVE'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CartItems"
                ADD CONSTRAINT "CK_CartItems_Status"
                CHECK ("Status" IS NULL OR "Status" IN ('AVAILABLE','UNAVAILABLE'));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "CartItems" DROP CONSTRAINT IF EXISTS "CK_CartItems_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Carts" DROP CONSTRAINT IF EXISTS "CK_Carts_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "CK_Users_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Reports" DROP CONSTRAINT IF EXISTS "CK_Reports_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "ShippingRequests" DROP CONSTRAINT IF EXISTS "CK_ShippingRequests_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "EscrowContracts" DROP CONSTRAINT IF EXISTS "CK_EscrowContracts_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Payments" DROP CONSTRAINT IF EXISTS "CK_Payments_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "CK_Orders_Status";""");
            migrationBuilder.Sql("""ALTER TABLE "Listings" DROP CONSTRAINT IF EXISTS "CK_Listings_ListingType";""");
            migrationBuilder.Sql("""ALTER TABLE "Listings" DROP CONSTRAINT IF EXISTS "CK_Listings_Status";""");
        }
    }
}
