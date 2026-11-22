using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImproveWarehouseStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinQuantity",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityBeforeConfirm",
                table: "StocktakeDetails",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinQuantity",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "QuantityBeforeConfirm",
                table: "StocktakeDetails");
        }
    }
}
