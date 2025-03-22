using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commers.Migrations
{
    /// <inheritdoc />
    public partial class updateentiteis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted_At",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Deleted_At",
                table: "ProductInventory");

            migrationBuilder.DropColumn(
                name: "Deleted_At",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "Deleted_At",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Deleted_At",
                table: "Discount");

            migrationBuilder.AddColumn<int>(
                name: "CartId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified_At = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cart_AspNetUsers_customerId",
                        column: x => x.customerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "deleteOpreations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Discription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deleteOpreations", x => x.id);
                    table.ForeignKey(
                        name: "FK_deleteOpreations_AspNetUsers_userid",
                        column: x => x.userid,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "updateOpreations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Discription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_updateOpreations", x => x.id);
                    table.ForeignKey(
                        name: "FK_updateOpreations_AspNetUsers_userid",
                        column: x => x.userid,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CartId",
                table: "Items",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_customerId",
                table: "Cart",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "IX_deleteOpreations_userid",
                table: "deleteOpreations",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_updateOpreations_userid",
                table: "updateOpreations",
                column: "userid");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Cart_CartId",
                table: "Items",
                column: "CartId",
                principalTable: "Cart",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Cart_CartId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Cart");

            migrationBuilder.DropTable(
                name: "deleteOpreations");

            migrationBuilder.DropTable(
                name: "updateOpreations");

            migrationBuilder.DropIndex(
                name: "IX_Items_CartId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CartId",
                table: "Items");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted_At",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted_At",
                table: "ProductInventory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted_At",
                table: "ProductCategory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted_At",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted_At",
                table: "Discount",
                type: "datetime2",
                nullable: true);
        }
    }
}
