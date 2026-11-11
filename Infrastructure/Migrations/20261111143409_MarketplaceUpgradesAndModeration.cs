using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceUpgradesAndModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AktifMi",
                table: "Yorumlar",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "SaticiProfilleri",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdminDogrulandiMi",
                table: "SaticiProfilleri",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KapakResimUrl",
                table: "SaticiProfilleri",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ilce",
                table: "AliciProfilleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sehir",
                table: "AliciProfilleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AktifMi",
                table: "Yorumlar");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "AdminDogrulandiMi",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "KapakResimUrl",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "Ilce",
                table: "AliciProfilleri");

            migrationBuilder.DropColumn(
                name: "Sehir",
                table: "AliciProfilleri");
        }
    }
}
