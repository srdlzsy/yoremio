using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditHardeningChatContextAndPlanliGonderim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Talepler",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsletmeDogrulandiMi",
                table: "SaticiProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UreticiDogrulandiMi",
                table: "SaticiProfilleri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TalepId",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UrunId",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanliGonderimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaticiId = table.Column<string>(type: "text", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MenseiSehir = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    HedefSehir = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    TahminiUrunNotu = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    GonderimTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SonTalepTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SogukZincirVarMi = table.Column<bool>(type: "boolean", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanliGonderimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanliGonderimler_SaticiProfilleri_SaticiId",
                        column: x => x.SaticiId,
                        principalTable: "SaticiProfilleri",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_TalepId",
                table: "ChatMessages",
                column: "TalepId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UrunId",
                table: "ChatMessages",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanliGonderimler_HedefSehir_AktifMi_GonderimTarihi",
                table: "PlanliGonderimler",
                columns: new[] { "HedefSehir", "AktifMi", "GonderimTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanliGonderimler_SaticiId",
                table: "PlanliGonderimler",
                column: "SaticiId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Talepler_TalepId",
                table: "ChatMessages",
                column: "TalepId",
                principalTable: "Talepler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Urunler_UrunId",
                table: "ChatMessages",
                column: "UrunId",
                principalTable: "Urunler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Talepler_TalepId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Urunler_UrunId",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "PlanliGonderimler");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_TalepId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_UrunId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "IsletmeDogrulandiMi",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "UreticiDogrulandiMi",
                table: "SaticiProfilleri");

            migrationBuilder.DropColumn(
                name: "TalepId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "UrunId",
                table: "ChatMessages");
        }
    }
}
