using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadesMedidaAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "pulse",
                table: "clientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "unidades_medida",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Unidades = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades_medida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unidades_medida_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "pulse",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidad_medida_local_mappings",
                schema: "pulse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalId = table.Column<long>(type: "bigint", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidad_medida_local_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unidad_medida_local_mappings_unidades_medida_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "pulse",
                        principalTable: "unidades_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unidad_medida_local_mappings_TenantId_LocalId",
                schema: "pulse",
                table: "unidad_medida_local_mappings",
                columns: new[] { "TenantId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unidad_medida_local_mappings_UnidadId",
                schema: "pulse",
                table: "unidad_medida_local_mappings",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_medida_TenantId_UpdatedAt",
                schema: "pulse",
                table: "unidades_medida",
                columns: new[] { "TenantId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unidad_medida_local_mappings",
                schema: "pulse");

            migrationBuilder.DropTable(
                name: "unidades_medida",
                schema: "pulse");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "pulse",
                table: "clientes");
        }
    }
}
