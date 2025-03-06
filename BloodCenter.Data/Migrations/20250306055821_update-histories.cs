using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodCenter.Data.Migrations
{
    public partial class updatehistories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActivityId",
                table: "Histories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "StatusHistories",
                table: "Histories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "StatusHistories",
                table: "Histories");
        }
    }
}
