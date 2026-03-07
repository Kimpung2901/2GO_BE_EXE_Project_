using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAiImageVisionCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiImageVisionCaches",
                columns: table => new
                {
                    CacheId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    QualityScore = table.Column<int>(type: "integer", nullable: true),
                    DamageLabels = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConditionLabel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RawResponse = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiImageVisionCache", x => x.CacheId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiImageVisionCache_ImageUrl",
                table: "AiImageVisionCaches",
                column: "ImageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiImageVisionCaches");
        }
    }
}
