using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SchemaHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "AliciProfilleri");

            migrationBuilder.CreateIndex(
                name: "IX_SaticiProfilleri_VergiNo",
                table: "SaticiProfilleri",
                column: "VergiNo",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Puanlar_PuanDegeri",
                table: "Puanlar",
                sql: "\"PuanDegeri\" BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_Adi",
                table: "Kategoriler",
                column: "Adi",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SaticiProfilleri_VergiNo",
                table: "SaticiProfilleri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Puanlar_PuanDegeri",
                table: "Puanlar");

            migrationBuilder.DropIndex(
                name: "IX_Kategoriler_Adi",
                table: "Kategoriler");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "AliciProfilleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "AliciProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "AliciProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "AliciProfilleri",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "AliciProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AliciProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "AliciProfilleri",
                type: "text",
                nullable: true);
        }
    }
}
