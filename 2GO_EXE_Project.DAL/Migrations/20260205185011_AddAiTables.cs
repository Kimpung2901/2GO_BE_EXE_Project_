using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIAnalysisLog",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAnalysisLog", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "ManualReviewQueue",
                columns: table => new
                {
                    QueueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListingId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualReviewQueue", x => x.QueueId);
                });

            migrationBuilder.CreateTable(
                name: "MarketPriceCache",
                columns: table => new
                {
                    ProductKey = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    MinPrice = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    AvgPrice = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    MaxPrice = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    SourcesJson = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceCache", x => x.ProductKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIAnalysisLog");

            migrationBuilder.DropTable(
                name: "ManualReviewQueue");

            migrationBuilder.DropTable(
                name: "MarketPriceCache");
        }
    }
}
