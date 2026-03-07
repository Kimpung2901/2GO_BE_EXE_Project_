using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    TransferId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayoutId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    TotalCredit = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovalState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.TransferId);
                    table.ForeignKey(
                        name: "FK_Transfers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "TransferTransactions",
                columns: table => new
                {
                    TransferTransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferId = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayoutTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ToBin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToAccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransactionDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferTransactions", x => x.TransferTransactionId);
                    table.ForeignKey(
                        name: "FK_TransferTransactions_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "TransferId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_UserId",
                table: "Transfers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTransactions_TransferId",
                table: "TransferTransactions",
                column: "TransferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferTransactions");

            migrationBuilder.DropTable(
                name: "Transfers");
        }
    }
}
