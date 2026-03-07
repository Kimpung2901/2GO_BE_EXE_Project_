using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingLabelUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "ShippingRequests" ADD COLUMN IF NOT EXISTS "Label52x70Url" character varying(500);
                ALTER TABLE "ShippingRequests" ADD COLUMN IF NOT EXISTS "Label80x80Url" character varying(500);
                ALTER TABLE "ShippingRequests" ADD COLUMN IF NOT EXISTS "LabelA5Url" character varying(500);
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosCheckoutUrl" character varying(500);
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosOrderCode" bigint;
                ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PayosPaymentLinkId" character varying(255);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label52x70Url",
                table: "ShippingRequests");

            migrationBuilder.DropColumn(
                name: "Label80x80Url",
                table: "ShippingRequests");

            migrationBuilder.DropColumn(
                name: "LabelA5Url",
                table: "ShippingRequests");

            migrationBuilder.DropColumn(
                name: "PayosCheckoutUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayosOrderCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayosPaymentLinkId",
                table: "Payments");
        }
    }
}
