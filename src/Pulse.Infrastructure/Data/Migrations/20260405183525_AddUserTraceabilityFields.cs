using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTraceabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                schema: "identity",
                table: "AspNetUsers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "local");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "identity",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                schema: "identity",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginAt",
                schema: "identity",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                schema: "identity",
                table: "AspNetUsers",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GoogleSubject",
                schema: "identity",
                table: "AspNetUsers",
                column: "GoogleSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GoogleSubject",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
