using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Bildirimler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<string>(type: "text", nullable: false),
                    Tur = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Baslik = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Mesaj = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IlgiliVarlikTuru = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IlgiliVarlikId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AksiyonUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId_OkunmaTarihi_OlusturmaTarihi",
                table: "Bildirimler",
                columns: new[] { "KullaniciId", "OkunmaTarihi", "OlusturmaTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildirimler");
        }
    }
}
