using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260209094500_AddSubscriptionPlanCodeAndListingPublishedAt")]
    public partial class AddSubscriptionPlanCodeAndListingPublishedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlanCode",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanCode",
                table: "Payments");
        }
    }
}
