using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_CreatedAt",
                table: "StockTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ProductId",
                table: "StockTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ReferenceType_ReferenceId",
                table: "StockTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_WarehouseId_ProductId_CreatedAt",
                table: "StockTransactions",
                columns: new[] { "WarehouseId", "ProductId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransactions");
        }
    }
}
