using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReportEvidenceUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceUrls",
                table: "Reports",
                type: "character varying(4000)",
                unicode: false,
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenceUrls",
                table: "Reports");
        }
    }
}
