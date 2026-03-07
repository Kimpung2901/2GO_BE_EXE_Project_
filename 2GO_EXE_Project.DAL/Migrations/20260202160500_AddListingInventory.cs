using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260202160500_AddListingInventory")]
    public partial class AddListingInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "Listings",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ListingType",
                table: "Listings",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                defaultValue: "SINGLE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingType",
                table: "Listings");
        }
    }
}
