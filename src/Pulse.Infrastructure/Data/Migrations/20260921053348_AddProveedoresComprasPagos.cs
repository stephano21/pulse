using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProveedoresComprasPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proveedores",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeudaInicial = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proveedores_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "pulse",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_proveedor",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Nota = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_proveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compras_proveedor_proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalSchema: "pulse",
                        principalTable: "proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_proveedor_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "pulse",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_proveedor",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Nota = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComprobanteFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_proveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagos_proveedor_proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalSchema: "pulse",
                        principalTable: "proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_proveedor_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "pulse",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proveedor_local_mappings",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalId = table.Column<long>(type: "bigint", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedor_local_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proveedor_local_mappings_proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalSchema: "pulse",
                        principalTable: "proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "compra_proveedor_local_mappings",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalId = table.Column<long>(type: "bigint", nullable: false),
                    CompraId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_proveedor_local_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compra_proveedor_local_mappings_compras_proveedor_CompraId",
                        column: x => x.CompraId,
                        principalSchema: "pulse",
                        principalTable: "compras_proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pago_proveedor_local_mappings",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalId = table.Column<long>(type: "bigint", nullable: false),
                    PagoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pago_proveedor_local_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pago_proveedor_local_mappings_pagos_proveedor_PagoId",
                        column: x => x.PagoId,
                        principalSchema: "pulse",
                        principalTable: "pagos_proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compra_proveedor_local_mappings_CompraId",
                schema: "pulse",
                table: "compra_proveedor_local_mappings",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_compra_proveedor_local_mappings_TenantId_LocalId",
                schema: "pulse",
                table: "compra_proveedor_local_mappings",
                columns: new[] { "TenantId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_proveedor_ProveedorId",
                schema: "pulse",
                table: "compras_proveedor",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_compras_proveedor_TenantId_CreatedAt",
                schema: "pulse",
                table: "compras_proveedor",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pago_proveedor_local_mappings_PagoId",
                schema: "pulse",
                table: "pago_proveedor_local_mappings",
                column: "PagoId");

            migrationBuilder.CreateIndex(
                name: "IX_pago_proveedor_local_mappings_TenantId_LocalId",
                schema: "pulse",
                table: "pago_proveedor_local_mappings",
                columns: new[] { "TenantId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_proveedor_ProveedorId",
                schema: "pulse",
                table: "pagos_proveedor",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_proveedor_TenantId_CreatedAt",
                schema: "pulse",
                table: "pagos_proveedor",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_proveedor_TenantId_Fecha",
                schema: "pulse",
                table: "pagos_proveedor",
                columns: new[] { "TenantId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_proveedor_local_mappings_ProveedorId",
                schema: "pulse",
                table: "proveedor_local_mappings",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_proveedor_local_mappings_TenantId_LocalId",
                schema: "pulse",
                table: "proveedor_local_mappings",
                columns: new[] { "TenantId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_TenantId_UpdatedAt",
                schema: "pulse",
                table: "proveedores",
                columns: new[] { "TenantId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compra_proveedor_local_mappings",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "pago_proveedor_local_mappings",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "proveedor_local_mappings",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "compras_proveedor",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "pagos_proveedor",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "proveedores",
                schema: "pulse");
        }
    }
}
