using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260206130000_AddChatbotLogs")]
    public partial class AddChatbotLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotLogs",
                columns: table => new
                {
                    ChatbotLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Question = table.Column<string>(type: "text", nullable: true),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    MatchedIntent = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    Confidence = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotLogs", x => x.ChatbotLogId);
                    table.ForeignKey(
                        name: "FK_ChatbotLogs_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotLogs_UserId",
                table: "ChatbotLogs",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotLogs");
        }
    }
}