using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReportOrderDisputeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "Reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WaitingForUserId",
                table: "Reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_OrderId",
                table: "Reports",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Orders_OrderId",
                table: "Reports",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Orders_OrderId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_OrderId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "WaitingForUserId",
                table: "Reports");
        }
    }
}
