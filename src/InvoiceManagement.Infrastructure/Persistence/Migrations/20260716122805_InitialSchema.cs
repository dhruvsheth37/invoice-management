using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InvoiceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceStatuses",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.CheckConstraint("CK_Tenants_Name_NotBlank", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Tenants_Slug_NotBlank", "LEN(LTRIM(RTRIM([Slug]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Customers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "varchar(100)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseStatus = table.Column<short>(type: "smallint", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(64)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceNumberSequences",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceNumberSequences", x => new { x.TenantId, x.FiscalYear });
                    table.CheckConstraint("CK_InvoiceNumberSequences_Value", "[CurrentValue] >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceNumberSequences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLocations", x => x.Id);
                    table.UniqueConstraint("AK_CustomerLocations_TenantId_CustomerId_Id", x => new { x.TenantId, x.CustomerId, x.Id });
                    table.UniqueConstraint("AK_CustomerLocations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CustomerLocations_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillToCustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillToLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillToTaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillToAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillToAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillToCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillToStateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillToPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillToCountryCode = table.Column<string>(type: "char(2)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.UniqueConstraint("AK_Invoices_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Invoices_Amounts", "[Subtotal] >= 0 AND [TaxTotal] >= 0 AND [Total] >= 0 AND [Total] = [Subtotal] + [TaxTotal]");
                    table.CheckConstraint("CK_Invoices_Dates", "[DueDate] IS NULL OR [IssueDate] IS NULL OR [DueDate] >= [IssueDate]");
                    table.CheckConstraint("CK_Invoices_Deactivation", "[IsActive] = 1 OR [StatusId] = 1");
                    table.CheckConstraint("CK_Invoices_Draft", "[StatusId] <> 1 OR ([InvoiceNumber] IS NULL AND [IssueDate] IS NULL AND [PaidDate] IS NULL)");
                    table.CheckConstraint("CK_Invoices_IssuedSnapshot", "[StatusId] NOT IN (2, 3) OR ([InvoiceNumber] IS NOT NULL AND [IssueDate] IS NOT NULL AND [DueDate] IS NOT NULL AND [BillToCustomerCode] IS NOT NULL AND [BillToLegalName] IS NOT NULL AND [BillToAddressLine1] IS NOT NULL AND [BillToCity] IS NOT NULL AND [BillToCountryCode] IS NOT NULL)");
                    table.CheckConstraint("CK_Invoices_Paid", "([StatusId] = 3 AND [PaidDate] IS NOT NULL) OR ([StatusId] <> 3 AND [PaidDate] IS NULL)");
                    table.CheckConstraint("CK_Invoices_Void", "([StatusId] = 4 AND [VoidReason] IS NOT NULL) OR ([StatusId] <> 4 AND [VoidReason] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Invoices_CustomerLocations_TenantId_CustomerId_CustomerLocationId",
                        columns: x => new { x.TenantId, x.CustomerId, x.CustomerLocationId },
                        principalTable: "CustomerLocations",
                        principalColumns: new[] { "TenantId", "CustomerId", "Id" });
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" });
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "InvoiceStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidToUtc")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFromUtc");

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.UniqueConstraint("AK_InvoiceLineItems_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_InvoiceLineItems_Amounts", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]");
                    table.CheckConstraint("CK_InvoiceLineItems_Values", "[LineNumber] > 0 AND [Quantity] > 0 AND [UnitPrice] >= 0 AND [TaxRate] >= 0 AND [TaxRate] <= 1");
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "Invoices",
                        principalColumns: new[] { "TenantId", "Id" });
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoiceLineItemsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidToUtc")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFromUtc");

            migrationBuilder.CreateTable(
                name: "InvoiceStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<byte>(type: "tinyint", nullable: true),
                    ToStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceStatusHistory_InvoiceStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "InvoiceStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceStatusHistory_InvoiceStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "InvoiceStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvoiceStatusHistory_Invoices_TenantId_InvoiceId",
                        columns: x => new { x.TenantId, x.InvoiceId },
                        principalTable: "Invoices",
                        principalColumns: new[] { "TenantId", "Id" });
                });

            migrationBuilder.InsertData(
                table: "InvoiceStatuses",
                columns: new[] { "Id", "Code", "DisplayName", "SortOrder" },
                values: new object[,]
                {
                    { (byte)1, "Draft", "Draft", (byte)1 },
                    { (byte)2, "Issued", "Issued", (byte)2 },
                    { (byte)3, "Paid", "Paid", (byte)3 },
                    { (byte)4, "Void", "Void", (byte)4 }
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedBy", "CreatedUtc", "IsActive", "ModifiedBy", "ModifiedUtc", "Name", "Slug" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Demo Tenant", "demo" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLocations_TenantId_CustomerId_Name",
                table: "CustomerLocations",
                columns: new[] { "TenantId", "CustomerId", "Name" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Code",
                table: "Customers",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_IsActive_LegalName",
                table: "Customers",
                columns: new[] { "TenantId", "IsActive", "LegalName" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_ExpiresUtc",
                table: "IdempotencyRequests",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_TenantId_Operation_IdempotencyKey",
                table: "IdempotencyRequests",
                columns: new[] { "TenantId", "Operation", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_TenantId_InvoiceId_LineNumber",
                table: "InvoiceLineItems",
                columns: new[] { "TenantId", "InvoiceId", "LineNumber" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StatusId",
                table: "Invoices",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_CustomerId_CustomerLocationId",
                table: "Invoices",
                columns: new[] { "TenantId", "CustomerId", "CustomerLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_InvoiceNumber",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true,
                filter: "[InvoiceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_CustomerId_CreatedUtc",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "CustomerId", "CreatedUtc" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_StatusId_CreatedUtc_Id",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "StatusId", "CreatedUtc", "Id" },
                descending: new[] { false, false, false, true, true })
                .Annotation("SqlServer:Include", new[] { "InvoiceNumber", "CustomerId", "Total", "CurrencyCode", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_IsActive_StatusId_DueDate",
                table: "Invoices",
                columns: new[] { "TenantId", "IsActive", "StatusId", "DueDate" })
                .Annotation("SqlServer:Include", new[] { "CurrencyCode", "Total" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStatuses_Code",
                table: "InvoiceStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStatusHistory_FromStatusId",
                table: "InvoiceStatusHistory",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStatusHistory_TenantId_InvoiceId_ChangedUtc",
                table: "InvoiceStatusHistory",
                columns: new[] { "TenantId", "InvoiceId", "ChangedUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStatusHistory_ToStatusId",
                table: "InvoiceStatusHistory",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRequests");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoiceLineItemsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidToUtc")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFromUtc");

            migrationBuilder.DropTable(
                name: "InvoiceNumberSequences");

            migrationBuilder.DropTable(
                name: "InvoiceStatusHistory");

            migrationBuilder.DropTable(
                name: "Invoices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidToUtc")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFromUtc");

            migrationBuilder.DropTable(
                name: "CustomerLocations");

            migrationBuilder.DropTable(
                name: "InvoiceStatuses");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
