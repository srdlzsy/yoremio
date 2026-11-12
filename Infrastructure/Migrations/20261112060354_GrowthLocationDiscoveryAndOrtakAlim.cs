using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GrowthLocationDiscoveryAndOrtakAlim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Talepler_Urunler_UrunId",
                table: "Talepler");

            migrationBuilder.AddColumn<string>(
                name: "BirimMiktar",
                table: "Urunler",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EldenTeslimVarMi",
                table: "Urunler",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "GonderimSehirleri",
                table: "Urunler",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoruntulenmeSayisi",
                table: "Urunler",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "KargoVarMi",
                table: "Urunler",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TeslimatSecenekleri",
                table: "Urunler",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UretimIlce",
                table: "Urunler",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UretimSehir",
                table: "Urunler",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "YeniHasatMi",
                table: "Urunler",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GonderimSehir",
                table: "TalepTeklifler",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeTarihi",
                table: "TalepTeklifler",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KargoDahilMi",
                table: "TalepTeklifler",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TeslimatSuresiGun",
                table: "TalepTeklifler",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UrunId",
                table: "Talepler",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "AliciGerceklestiOnayi",
                table: "Talepler",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArananUrunAdi",
                table: "Talepler",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "Talepler",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HedefBirimFiyat",
                table: "Talepler",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KategoriId",
                table: "Talepler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SaticiGerceklestiOnayi",
                table: "Talepler",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonTeklifTarihi",
                table: "Talepler",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TalepTipi",
                table: "Talepler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "BIREYSEL");

            migrationBuilder.AddColumn<string>(
                name: "TalepTuru",
                table: "Talepler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "URUN");

            migrationBuilder.AddColumn<string>(
                name: "TercihEdilenSehirler",
                table: "Talepler",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeslimatSehir",
                table: "Talepler",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaketTuru",
                table: "SaticiProfilleri",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "UCRETSIZ");

            migrationBuilder.CreateTable(
                name: "FiyatAlarmlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciId = table.Column<string>(type: "text", nullable: false),
                    UrunId = table.Column<int>(type: "integer", nullable: false),
                    HedefFiyat = table.Column<decimal>(type: "numeric", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    SonTetiklenenFiyat = table.Column<decimal>(type: "numeric", nullable: true),
                    SonTetiklemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiyatAlarmlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiyatAlarmlari_AspNetUsers_AliciId",
                        column: x => x.AliciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiyatAlarmlari_Urunler_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urunler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrtakAlimlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaticiId = table.Column<string>(type: "text", nullable: false),
                    OlusturanKullaniciId = table.Column<string>(type: "text", nullable: true),
                    UrunId = table.Column<int>(type: "integer", nullable: true),
                    KategoriId = table.Column<int>(type: "integer", nullable: true),
                    Baslik = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    MenseiSehir = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    HedefSehir = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric", nullable: false),
                    Birim = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MinimumKatilimci = table.Column<int>(type: "integer", nullable: false),
                    SogukZincirGerekliMi = table.Column<bool>(type: "boolean", nullable: false),
                    TeslimatKosullari = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IptalKosullari = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SonKatilimTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrtakAlimlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrtakAlimlar_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrtakAlimlar_SaticiProfilleri_SaticiId",
                        column: x => x.SaticiId,
                        principalTable: "SaticiProfilleri",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrtakAlimlar_Urunler_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urunler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SaticiTakipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciId = table.Column<string>(type: "text", nullable: false),
                    SaticiId = table.Column<string>(type: "text", nullable: false),
                    BildirimAcikMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaticiTakipleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaticiTakipleri_AspNetUsers_AliciId",
                        column: x => x.AliciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaticiTakipleri_SaticiProfilleri_SaticiId",
                        column: x => x.SaticiId,
                        principalTable: "SaticiProfilleri",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokAlarmlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AliciId = table.Column<string>(type: "text", nullable: false),
                    UrunId = table.Column<int>(type: "integer", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    SonTetiklemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokAlarmlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokAlarmlari_AspNetUsers_AliciId",
                        column: x => x.AliciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StokAlarmlari_Urunler_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urunler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalepEslesmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TalepId = table.Column<int>(type: "integer", nullable: false),
                    SaticiId = table.Column<string>(type: "text", nullable: false),
                    EslesmeSkoru = table.Column<int>(type: "integer", nullable: false),
                    Sebep = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalepEslesmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalepEslesmeleri_SaticiProfilleri_SaticiId",
                        column: x => x.SaticiId,
                        principalTable: "SaticiProfilleri",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalepEslesmeleri_Talepler_TalepId",
                        column: x => x.TalepId,
                        principalTable: "Talepler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrtakAlimKatilimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrtakAlimId = table.Column<int>(type: "integer", nullable: false),
                    AliciId = table.Column<string>(type: "text", nullable: false),
                    KatilimMiktari = table.Column<int>(type: "integer", nullable: false),
                    Not = table.Column<string>(type: "text", nullable: true),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrtakAlimKatilimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrtakAlimKatilimlari_AspNetUsers_AliciId",
                        column: x => x.AliciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrtakAlimKatilimlari_OrtakAlimlar_OrtakAlimId",
                        column: x => x.OrtakAlimId,
                        principalTable: "OrtakAlimlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Talepler_KategoriId",
                table: "Talepler",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Talepler_TalepTuru_Durum_OlusturmaTarihi",
                table: "Talepler",
                columns: new[] { "TalepTuru", "Durum", "OlusturmaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_FiyatAlarmlari_AliciId_UrunId",
                table: "FiyatAlarmlari",
                columns: new[] { "AliciId", "UrunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiyatAlarmlari_UrunId",
                table: "FiyatAlarmlari",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_OrtakAlimKatilimlari_AliciId",
                table: "OrtakAlimKatilimlari",
                column: "AliciId");

            migrationBuilder.CreateIndex(
                name: "IX_OrtakAlimKatilimlari_OrtakAlimId_AliciId",
                table: "OrtakAlimKatilimlari",
                columns: new[] { "OrtakAlimId", "AliciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrtakAlimlar_KategoriId",
                table: "OrtakAlimlar",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_OrtakAlimlar_SaticiId",
                table: "OrtakAlimlar",
                column: "SaticiId");

            migrationBuilder.CreateIndex(
                name: "IX_OrtakAlimlar_UrunId",
                table: "OrtakAlimlar",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_SaticiTakipleri_AliciId_SaticiId",
                table: "SaticiTakipleri",
                columns: new[] { "AliciId", "SaticiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaticiTakipleri_SaticiId",
                table: "SaticiTakipleri",
                column: "SaticiId");

            migrationBuilder.CreateIndex(
                name: "IX_StokAlarmlari_AliciId_UrunId",
                table: "StokAlarmlari",
                columns: new[] { "AliciId", "UrunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokAlarmlari_UrunId",
                table: "StokAlarmlari",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_TalepEslesmeleri_SaticiId",
                table: "TalepEslesmeleri",
                column: "SaticiId");

            migrationBuilder.CreateIndex(
                name: "IX_TalepEslesmeleri_TalepId_SaticiId",
                table: "TalepEslesmeleri",
                columns: new[] { "TalepId", "SaticiId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Talepler_Kategoriler_KategoriId",
                table: "Talepler",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Talepler_Urunler_UrunId",
                table: "Talepler",
                column: "UrunId",
                principalTable: "Urunler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Talepler_Kategoriler_KategoriId",
                table: "Talepler");

            migrationBuilder.DropForeignKey(
                name: "FK_Talepler_Urunler_UrunId",
                table: "Talepler");

            migrationBuilder.DropTable(
                name: "FiyatAlarmlari");

            migrationBuilder.DropTable(
                name: "OrtakAlimKatilimlari");

            migrationBuilder.DropTable(
                name: "SaticiTakipleri");

            migrationBuilder.DropTable(
                name: "StokAlarmlari");

            migrationBuilder.DropTable(
                name: "TalepEslesmeleri");

            migrationBuilder.DropTable(
                name: "OrtakAlimlar");

            migrationBuilder.DropIndex(
                name: "IX_Talepler_KategoriId",
                table: "Talepler");

            migrationBuilder.DropIndex(
                name: "IX_Talepler_TalepTuru_Durum_OlusturmaTarihi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "BirimMiktar",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "EldenTeslimVarMi",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "GonderimSehirleri",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "GoruntulenmeSayisi",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "KargoVarMi",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "TeslimatSecenekleri",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "UretimIlce",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "UretimSehir",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "YeniHasatMi",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "GonderimSehir",
                table: "TalepTeklifler");

            migrationBuilder.DropColumn(
                name: "GuncellemeTarihi",
                table: "TalepTeklifler");

            migrationBuilder.DropColumn(
                name: "KargoDahilMi",
                table: "TalepTeklifler");

            migrationBuilder.DropColumn(
                name: "TeslimatSuresiGun",
                table: "TalepTeklifler");

            migrationBuilder.DropColumn(
                name: "AliciGerceklestiOnayi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "ArananUrunAdi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "Birim",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "HedefBirimFiyat",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "KategoriId",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "SaticiGerceklestiOnayi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "SonTeklifTarihi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "TalepTipi",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "TalepTuru",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "TercihEdilenSehirler",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "TeslimatSehir",
                table: "Talepler");

            migrationBuilder.DropColumn(
                name: "PaketTuru",
                table: "SaticiProfilleri");

            migrationBuilder.AlterColumn<int>(
                name: "UrunId",
                table: "Talepler",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Talepler_Urunler_UrunId",
                table: "Talepler",
                column: "UrunId",
                principalTable: "Urunler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
