using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class YoremioMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "AliciProfilleri");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AliciProfilleri",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
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

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SaticiProfilleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "SaticiProfilleri",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AliciProfilleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "AliciProfilleri",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
