using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseIntegerAuditUserIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Tenants]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

                UPDATE [Customers]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

                UPDATE [CustomerLocations]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

                UPDATE [InvoiceStatusHistory]
                SET [ChangedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ChangedBy])), N'1');

                ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = OFF);

                UPDATE [Invoices]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

                EXEC(N'UPDATE [history].[InvoicesHistory]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N''1''),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N''1'');');

                ALTER TABLE [Invoices]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]));

                ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = OFF);

                UPDATE [InvoiceLineItems]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

                EXEC(N'UPDATE [history].[InvoiceLineItemsHistory]
                SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N''1''),
                    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N''1'');');

                ALTER TABLE [InvoiceLineItems]
                    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]));
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ModifiedBy",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "ChangedBy",
                table: "InvoiceStatusHistory",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "ModifiedBy",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "ModifiedBy",
                table: "InvoiceLineItems",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "InvoiceLineItems",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "ModifiedBy",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "ModifiedBy",
                table: "CustomerLocations",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "CustomerLocations",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedBy", "ModifiedBy" },
                values: new object[] { 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "InvoiceStatusHistory",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "InvoiceLineItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "InvoiceLineItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "CustomerLocations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "CustomerLocations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedBy", "ModifiedBy" },
                values: new object[] { "seed", null });
        }
    }
}
