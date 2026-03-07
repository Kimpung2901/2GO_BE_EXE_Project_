using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndCommissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FreeListingUsed",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionBaseAmount",
                table: "Payments",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Payments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionDays",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionValidFrom",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionValidUntil",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeListingUsed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CommissionBaseAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubscriptionDays",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubscriptionValidFrom",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubscriptionValidUntil",
                table: "Payments");
        }
    }
}
