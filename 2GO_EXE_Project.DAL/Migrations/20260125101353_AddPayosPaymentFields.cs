using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPayosPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosCheckoutUrl" character varying(500);
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosOrderCode" bigint;
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosPaymentLinkId" character varying(255);
                ALTER TABLE "PaymentLogs" ADD COLUMN IF NOT EXISTS "Event" character varying(50);
                ALTER TABLE "PaymentLogs" ADD COLUMN IF NOT EXISTS "Provider" character varying(50);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayosCheckoutUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayosOrderCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayosPaymentLinkId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Event",
                table: "PaymentLogs");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "PaymentLogs");
        }
    }
}
