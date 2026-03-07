using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingRequestUniqueOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShippingRequests_OrderId",
                table: "ShippingRequests");

            migrationBuilder.CreateIndex(
                name: "UX_ShippingRequests_OrderId",
                table: "ShippingRequests",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ShippingRequests_OrderId",
                table: "ShippingRequests");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRequests_OrderId",
                table: "ShippingRequests",
                column: "OrderId");
        }
    }
}
