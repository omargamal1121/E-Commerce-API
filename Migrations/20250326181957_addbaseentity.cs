using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commers.Migrations
{
    /// <inheritdoc />
    public partial class addbaseentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "adminOperationsLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "adminOperationsLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "adminOperationsLogs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "adminOperationsLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "adminOperationsLogs");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "adminOperationsLogs");
        }
    }
}
