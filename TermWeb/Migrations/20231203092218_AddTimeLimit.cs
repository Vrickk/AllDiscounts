using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TermWeb.Migrations
{
    public partial class AddTimeLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedEditAttempts",
                table: "Article",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Article",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedEditAttempts",
                table: "Article");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Article");
        }
    }
}
