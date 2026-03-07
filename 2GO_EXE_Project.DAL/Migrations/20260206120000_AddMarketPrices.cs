using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260206120000_AddMarketPrices")]
    public partial class AddMarketPrices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketPriceCache");

            migrationBuilder.CreateTable(
                name: "MarketPrices",
                columns: table => new
                {
                    MarketPriceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductKey = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    Condition = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    AvgPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    MinPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    MaxPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    Confidence = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPrices", x => x.MarketPriceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Product_Category_Condition",
                table: "MarketPrices",
                columns: new[] { "ProductKey", "CategoryId", "Condition" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketPrices");

            migrationBuilder.CreateTable(
                name: "MarketPriceCache",
                columns: table => new
                {
                    ProductKey = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    AvgPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    MaxPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    MinPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: false),
                    SourcesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceCache", x => x.ProductKey);
                });
        }
    }
}