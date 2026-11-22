using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportReceiptSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ExportReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Đơn FreshMart cũ → Source = Store (1)
            migrationBuilder.Sql("""
                UPDATE ExportReceipts
                SET Source = 1
                WHERE Note LIKE N'%FreshMart%' OR Note LIKE N'%đơn hàng FreshMart%'
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_CreatedAt",
                table: "ExportReceipts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_Source",
                table: "ExportReceipts",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_CreatedAt",
                table: "ExportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_Source",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ExportReceipts");
        }
    }
}
