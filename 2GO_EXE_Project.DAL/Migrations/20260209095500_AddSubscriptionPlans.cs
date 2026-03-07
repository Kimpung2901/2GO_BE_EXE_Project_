using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using _2GO_EXE_Project.DAL.Context;

#nullable disable

namespace _2GO_EXE_Project.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260209095500_AddSubscriptionPlans")]
    public partial class AddSubscriptionPlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    MonthlyListingLimit = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanAudits",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanAudits", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanAudits_Plans",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanAudits_PlanId",
                table: "SubscriptionPlanAudits",
                column: "PlanId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlanAudits");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
