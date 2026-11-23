using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportPurchaseCostsAndInvoiceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NonRefundableTax",
                table: "ImportReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseInvoiceCode",
                table: "ImportReceipts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "ImportReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TradeDiscount",
                table: "ImportReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_PurchaseInvoiceCode",
                table: "ImportReceipts",
                column: "PurchaseInvoiceCode",
                unique: true,
                filter: "[PurchaseInvoiceCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_PurchaseInvoiceCode",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "NonRefundableTax",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceCode",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "TradeDiscount",
                table: "ImportReceipts");
        }
    }
}
