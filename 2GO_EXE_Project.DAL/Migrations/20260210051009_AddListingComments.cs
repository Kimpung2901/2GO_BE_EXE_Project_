using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddListingComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingComments",
                columns: table => new
                {
                    CommentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListingId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ListingC__D7C4A67F4A017A9D", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_ListingComments_ListingComments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ListingComments",
                        principalColumn: "CommentId");
                    table.ForeignKey(
                        name: "FK_ListingComments_Listings",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingComments_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_ListingId",
                table: "ListingComments",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_ParentId",
                table: "ListingComments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_UserId",
                table: "ListingComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingComments");
        }
    }
}
