using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addPayOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscrowContracts_Orders_OrderId",
                table: "EscrowContracts");

            migrationBuilder.DropIndex(
                name: "IX_Orders_EscrowId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_EscrowContracts_OrderId",
                table: "EscrowContracts");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrderCode",
                table: "Orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentExpiredAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentLinkId",
                table: "Orders",
                type: "character varying(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QrCodeUrl",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderTransaction",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderCode = table.Column<long>(type: "bigint", nullable: false),
                    PaymentLinkId = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: true),
                    Reference = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TransactionDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VirtualAccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VirtualAccountNumber = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    CounterAccountBankId = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    CounterAccountBankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CounterAccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CounterAccountNumber = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTransaction", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_OrderTransaction_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderInvoice",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    IssuedTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    IssuedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    ReservationCode = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    CodeOfTax = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInvoice", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_OrderInvoice_OrderTransaction_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "OrderTransaction",
                        principalColumn: "TransactionId");
                    table.ForeignKey(
                        name: "FK_OrderInvoice_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderInvoice_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "PaymentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EscrowId",
                table: "Orders",
                column: "EscrowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoice_OrderId",
                table: "OrderInvoice",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoice_PaymentId",
                table: "OrderInvoice",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoice_TransactionId",
                table: "OrderInvoice",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTransaction_OrderId",
                table: "OrderTransaction",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderInvoice");

            migrationBuilder.DropTable(
                name: "OrderTransaction");

            migrationBuilder.DropIndex(
                name: "IX_Orders_EscrowId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentExpiredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentLinkId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "QrCodeUrl",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EscrowId",
                table: "Orders",
                column: "EscrowId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowContracts_OrderId",
                table: "EscrowContracts",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_EscrowContracts_Orders_OrderId",
                table: "EscrowContracts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }
    }
}
