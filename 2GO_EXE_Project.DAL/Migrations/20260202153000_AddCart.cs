using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260202153000_AddCart")]
    public partial class AddCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    CartId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "ACTIVE"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Carts__51BCD7B7E05DE5F4", x => x.CartId);
                    table.ForeignKey(
                        name: "FK_Carts_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<long>(type: "bigint", nullable: true),
                    ListingId = table.Column<long>(type: "bigint", nullable: true),
                    SellerId = table.Column<long>(type: "bigint", nullable: true),
                    VariantId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    PriceSnapshot = table.Column<decimal>(type: "decimal(15, 2)", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "decimal(15, 2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: true),
                    VariantSnapshot = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    Status = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "AVAILABLE"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CartIte__488B0B0A3F05C2DE", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "CartId");
                    table.ForeignKey(
                        name: "FK_CartItems_Listings",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "ListingId");
                    table.ForeignKey(
                        name: "FK_CartItems_Sellers",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ListingId",
                table: "CartItems",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_SellerId",
                table: "CartItems",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Carts");
        }
    }
}
