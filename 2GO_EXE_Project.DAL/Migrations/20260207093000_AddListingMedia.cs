using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260207093000_AddListingMedia")]
    public partial class AddListingMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListingImages_Listings",
                table: "ListingImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ListingI__7516F70C45586241",
                table: "ListingImages");

            migrationBuilder.RenameTable(
                name: "ListingImages",
                newName: "ListingMedias");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                table: "ListingMedias",
                newName: "MediaId");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "ListingMedias",
                newName: "Url");

            migrationBuilder.RenameIndex(
                name: "IX_ListingImages_ListingId",
                table: "ListingMedias",
                newName: "IX_ListingMedias_ListingId");

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "ListingMedias",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                defaultValue: "IMAGE");

            migrationBuilder.Sql("UPDATE \"ListingMedias\" SET \"MediaType\" = 'IMAGE' WHERE \"MediaType\" IS NULL;");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ListingMedias",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK__ListingM__6D5D1F36E99C3C10",
                table: "ListingMedias",
                column: "MediaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListingMedias_Listings",
                table: "ListingMedias",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "ListingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListingMedias_Listings",
                table: "ListingMedias");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ListingM__6D5D1F36E99C3C10",
                table: "ListingMedias");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "ListingMedias");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ListingMedias");

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "ListingMedias",
                newName: "ImageId");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "ListingMedias",
                newName: "ImageUrl");

            migrationBuilder.RenameIndex(
                name: "IX_ListingMedias_ListingId",
                table: "ListingMedias",
                newName: "IX_ListingImages_ListingId");

            migrationBuilder.RenameTable(
                name: "ListingMedias",
                newName: "ListingImages");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ListingI__7516F70C45586241",
                table: "ListingImages",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListingImages_Listings",
                table: "ListingImages",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "ListingId");
        }
    }
}
