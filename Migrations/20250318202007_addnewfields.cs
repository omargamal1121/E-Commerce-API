using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commers.Migrations
{
    /// <inheritdoc />
    public partial class addnewfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "Products",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Products",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "ProductInventory",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "ProductInventory",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "Orders",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "Discount",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Discount",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "Categories",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Categories",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Modified_At",
                table: "Cart",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Cart",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProductInventory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Discount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Cart",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductInventory");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Discount");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Products",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Products",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "ProductInventory",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ProductInventory",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Orders",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Orders",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Discount",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Discount",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Categories",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Categories",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Cart",
                newName: "Modified_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Cart",
                newName: "Created_At");
        }
    }
}
