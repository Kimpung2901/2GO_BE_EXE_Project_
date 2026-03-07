using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRatingOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "UserRatings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserRatings_OrderId",
                table: "UserRatings",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRatings_Orders",
                table: "UserRatings",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRatings_Orders",
                table: "UserRatings");

            migrationBuilder.DropIndex(
                name: "UX_UserRatings_OrderId",
                table: "UserRatings");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "UserRatings");
        }
    }
}
