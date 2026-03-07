using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSubscriptionFieldsOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlanCode",
                table: "Users",
                type: "character varying(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionValidFrom",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionValidUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionPlanCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionValidFrom",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionValidUntil",
                table: "Users");
        }
    }
}
