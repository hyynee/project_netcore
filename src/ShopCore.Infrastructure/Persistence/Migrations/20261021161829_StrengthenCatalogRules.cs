using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenCatalogRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateTable(
                name: "ProductSearchTerms",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSearchTerms", x => new { x.ProductId, x.Term });
                    table.ForeignKey(
                        name: "FK_ProductSearchTerms_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products",
                sql: "[Price] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchTerms_Term_ProductId",
                table: "ProductSearchTerms",
                columns: new[] { "Term", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSearchTerms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Sample Category", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "CreatedBy", "Currency", "Description", "IsActive", "Name", "Price", "ReservedQuantity", "Stock", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "VND", "Seeded product used by the phase 03 vertical slice.", true, "Sample Product", 100000m, 0, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products",
                sql: "[Price] >= 0");
        }
    }
}
