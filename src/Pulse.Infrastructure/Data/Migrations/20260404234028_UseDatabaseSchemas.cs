using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseDatabaseSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "pulse");

            migrationBuilder.RenameTable(
                name: "ventas",
                newName: "ventas",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "venta_local_mappings",
                newName: "venta_local_mappings",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "venta_lineas",
                newName: "venta_lineas",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "tenants",
                newName: "tenants",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "products",
                newName: "products",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "product_local_mappings",
                newName: "product_local_mappings",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "processed_mutations",
                newName: "processed_mutations",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "idempotency_records",
                newName: "idempotency_records",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "cobros",
                newName: "cobros",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "cobro_local_mappings",
                newName: "cobro_local_mappings",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "clientes",
                newName: "clientes",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "cliente_local_mappings",
                newName: "cliente_local_mappings",
                newSchema: "pulse");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "identity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ventas",
                schema: "pulse",
                newName: "ventas");

            migrationBuilder.RenameTable(
                name: "venta_local_mappings",
                schema: "pulse",
                newName: "venta_local_mappings");

            migrationBuilder.RenameTable(
                name: "venta_lineas",
                schema: "pulse",
                newName: "venta_lineas");

            migrationBuilder.RenameTable(
                name: "tenants",
                schema: "pulse",
                newName: "tenants");

            migrationBuilder.RenameTable(
                name: "products",
                schema: "pulse",
                newName: "products");

            migrationBuilder.RenameTable(
                name: "product_local_mappings",
                schema: "pulse",
                newName: "product_local_mappings");

            migrationBuilder.RenameTable(
                name: "processed_mutations",
                schema: "pulse",
                newName: "processed_mutations");

            migrationBuilder.RenameTable(
                name: "idempotency_records",
                schema: "pulse",
                newName: "idempotency_records");

            migrationBuilder.RenameTable(
                name: "cobros",
                schema: "pulse",
                newName: "cobros");

            migrationBuilder.RenameTable(
                name: "cobro_local_mappings",
                schema: "pulse",
                newName: "cobro_local_mappings");

            migrationBuilder.RenameTable(
                name: "clientes",
                schema: "pulse",
                newName: "clientes");

            migrationBuilder.RenameTable(
                name: "cliente_local_mappings",
                schema: "pulse",
                newName: "cliente_local_mappings");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "identity",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "identity",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "identity",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "identity",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "identity",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "identity",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "identity",
                newName: "AspNetRoleClaims");
        }
    }
}
