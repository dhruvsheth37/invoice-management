using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_CreatedUtc_Id",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "CreatedUtc", "Id" },
                descending: new[] { false, false, true, true })
                .Annotation("SqlServer:Include", new[] { "InvoiceNumber", "StatusId", "CustomerId", "CurrencyCode", "Total", "IssueDate", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_CurrencyCode_StatusId_DueDate",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "CurrencyCode", "StatusId", "DueDate" })
                .Annotation("SqlServer:Include", new[] { "Total" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_DueDate_Id",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "DueDate", "Id" })
                .Annotation("SqlServer:Include", new[] { "InvoiceNumber", "StatusId", "CustomerId", "CurrencyCode", "Total", "IssueDate", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_Total_Id",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "Total", "Id" })
                .Annotation("SqlServer:Include", new[] { "InvoiceNumber", "StatusId", "CustomerId", "CurrencyCode", "IssueDate", "DueDate", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IsActive_CreatedUtc_Id",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IsActive_CurrencyCode_StatusId_DueDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IsActive_DueDate_Id",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_IsActive_Total_Id",
                table: "Invoices");
        }
    }
}
