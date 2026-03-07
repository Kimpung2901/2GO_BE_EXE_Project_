using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddEscrowDepositRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentStage",
                table: "Payments",
                type: "character varying(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositDeadlineAt",
                table: "EscrowContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositForfeitedAt",
                table: "EscrowContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositRate",
                table: "EscrowContracts",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositRefundedAt",
                table: "EscrowContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositReminderSentAt",
                table: "EscrowContracts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStage",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DepositDeadlineAt",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "DepositForfeitedAt",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "DepositRate",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "DepositRefundedAt",
                table: "EscrowContracts");

            migrationBuilder.DropColumn(
                name: "DepositReminderSentAt",
                table: "EscrowContracts");
        }
    }
}
