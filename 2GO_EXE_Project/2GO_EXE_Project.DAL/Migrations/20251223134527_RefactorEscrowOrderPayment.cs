using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEscrowOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "EscrowContracts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentId",
                table: "EscrowContracts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowContracts_OrderId",
                table: "EscrowContracts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowContracts_PaymentId",
                table: "EscrowContracts",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_EscrowContracts_Orders_OrderId",
                table: "EscrowContracts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_EscrowContracts_Payments_PaymentId",
                table: "EscrowContracts",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscrowContracts_Orders_OrderId",
                table: "EscrowContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_EscrowContracts_Payments_PaymentId",
                table: "EscrowContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_EscrowContracts_OrderId",
                table: "EscrowContracts");

            migrationBuilder.DropIndex(
                name: "IX_EscrowContracts_PaymentId",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "EscrowContracts");
        }
    }
}
