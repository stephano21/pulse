using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVentaReverso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoReverso",
                schema: "pulse",
                table: "ventas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversaDeVentaId",
                schema: "pulse",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversaDeVentaLineaId",
                schema: "pulse",
                table: "venta_lineas",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoReverso",
                schema: "pulse",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "ReversaDeVentaId",
                schema: "pulse",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "ReversaDeVentaLineaId",
                schema: "pulse",
                table: "venta_lineas");
        }
    }
}
