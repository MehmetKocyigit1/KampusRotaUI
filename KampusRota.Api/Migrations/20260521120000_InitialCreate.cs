using KampusRota.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KampusRota.Api.Migrations;

[DbContextAttribute(typeof(KampusRotaDbContext))]
[Migration("20260521120000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Kullanicilar",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Ad = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Soyad = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                SifreHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                TelefonNumarasi = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                OgrenciNumarasi = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                ProfilFotografiUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Cinsiyet = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                OrtalamaPuan = table.Column<double>(type: "REAL", nullable: false),
                Biyografi = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                OlusturulmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                OlusturanKullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                GuncellenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                GuncelleyenKullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                SilindiMi = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kullanicilar", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Yolculuklar",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SurucuId = table.Column<int>(type: "INTEGER", nullable: false),
                KalkisNoktasi = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                VarisNoktasi = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                KalkisZamani = table.Column<DateTime>(type: "TEXT", nullable: false),
                BosKoltukSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                KisiBasiUcret = table.Column<double>(type: "REAL", nullable: false),
                Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                SadeceKadinlarMi = table.Column<bool>(type: "INTEGER", nullable: false),
                OlusturulmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                OlusturanKullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                GuncellenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                GuncelleyenKullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                SilindiMi = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Yolculuklar", x => x.Id);
                table.ForeignKey(
                    name: "FK_Yolculuklar_Kullanicilar_SurucuId",
                    column: x => x.SurucuId,
                    principalTable: "Kullanicilar",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Kullanicilar_Email",
            table: "Kullanicilar",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Yolculuklar_SurucuId",
            table: "Yolculuklar",
            column: "SurucuId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Yolculuklar");
        migrationBuilder.DropTable(name: "Kullanicilar");
    }
}
