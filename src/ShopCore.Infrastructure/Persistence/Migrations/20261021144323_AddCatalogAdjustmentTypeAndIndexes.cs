using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAdjustmentTypeAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InventoryAdjustments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [InventoryAdjustments] SET [Type] = 'Correction' WHERE [Type] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "InventoryAdjustments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CategoryId_Name",
                table: "Products",
                columns: new[] { "IsActive", "CategoryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Name",
                table: "Products",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Price",
                table: "Products",
                columns: new[] { "IsActive", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_Name",
                table: "Categories",
                columns: new[] { "IsActive", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CategoryId_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Price",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "InventoryAdjustments");
        }
    }
}
