using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBanksAndBankBin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankBin",
                table: "UserProfiles",
                type: "character varying(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    BankId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: false),
                    Bin = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Banks__AA08CB13F44A7FD0", x => x.BankId);
                });

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Banks_Bin" ON "Banks" ("Bin");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Banks_Bin";
                """);

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropColumn(
                name: "BankBin",
                table: "UserProfiles");
        }
    }
}
