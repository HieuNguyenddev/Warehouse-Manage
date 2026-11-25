using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyLotAndAttrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId_ProductId",
                table: "ImportReceiptDetails");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseType",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ActiveIngredient",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DosageForm",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryAlertDays",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsControlledSubstance",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresColdStorage",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Strength",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "ImportReceiptDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "ImportReceiptDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDate",
                table: "ImportReceiptDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssuePurpose",
                table: "ExportReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LotAllocationsJson",
                table: "ExportReceiptDetails",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "ExportReceiptDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImportReceiptDetailId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_WarehouseType",
                table: "Warehouses",
                column: "WarehouseType");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ActiveIngredient",
                table: "Products",
                column: "ActiveIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsControlledSubstance",
                table: "Products",
                column: "IsControlledSubstance");

            migrationBuilder.CreateIndex(
                name: "IX_Products_RegistrationNumber",
                table: "Products",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId_ProductId_LotNumber",
                table: "ImportReceiptDetails",
                columns: new[] { "ImportReceiptId", "ProductId", "LotNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_IssuePurpose",
                table: "ExportReceipts",
                column: "IssuePurpose");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ExpiryDate_Quantity",
                table: "StockLots",
                columns: new[] { "ExpiryDate", "Quantity" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_ProductId",
                table: "StockLots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_WarehouseId_ProductId_LotNumber",
                table: "StockLots",
                columns: new[] { "WarehouseId", "ProductId", "LotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockLots");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_WarehouseType",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Products_ActiveIngredient",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsControlledSubstance",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_RegistrationNumber",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId_ProductId_LotNumber",
                table: "ImportReceiptDetails");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_IssuePurpose",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "WarehouseType",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ActiveIngredient",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DosageForm",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpiryAlertDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsControlledSubstance",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresColdStorage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "ManufactureDate",
                table: "ImportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "IssuePurpose",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "LotAllocationsJson",
                table: "ExportReceiptDetails");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "ExportReceiptDetails");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceiptDetails_ImportReceiptId_ProductId",
                table: "ImportReceiptDetails",
                columns: new[] { "ImportReceiptId", "ProductId" },
                unique: true);
        }
    }
}
