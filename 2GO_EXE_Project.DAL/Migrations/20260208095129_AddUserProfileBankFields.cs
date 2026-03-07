using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileBankFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"UserProfiles\" ADD COLUMN IF NOT EXISTS \"BankAccountName\" character varying(255);");
            migrationBuilder.Sql("ALTER TABLE \"UserProfiles\" ADD COLUMN IF NOT EXISTS \"BankAccountNumber\" character varying(50);");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MarketPrices",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "ProductKey",
                table: "MarketPrices",
                type: "character varying(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldUnicode: false,
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "MarketPrices",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.Sql(
                "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ChatbotLogs_Users') " +
                "THEN ALTER TABLE \"ChatbotLogs\" ADD CONSTRAINT \"FK_ChatbotLogs_Users\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"UserId\"); END IF; END $$;");

            migrationBuilder.Sql(
                "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Notifications_Users') " +
                "THEN ALTER TABLE \"Notifications\" ADD CONSTRAINT \"FK_Notifications_Users\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"UserId\"); END IF; END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotLogs_Users",
                table: "ChatbotLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MarketPrices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "ProductKey",
                table: "MarketPrices",
                type: "character varying(255)",
                unicode: false,
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldUnicode: false,
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "MarketPrices",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldUnicode: false,
                oldMaxLength: 20);
        }
    }
}
