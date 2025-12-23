using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBanUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanUntil",
                table: "Users");
        }
    }
}
