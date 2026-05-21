using KampusRota.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace KampusRota.Api.Migrations;

[DbContext(typeof(KampusRotaDbContext))]
public partial class KampusRotaDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11");

        modelBuilder.Entity("KampusRota.Api.Models.Kullanici", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<bool>("AktifMi").HasColumnType("INTEGER");
            entity.Property<string>("Ad").IsRequired().HasMaxLength(60).HasColumnType("TEXT");
            entity.Property<string>("Biyografi").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<string>("Cinsiyet").IsRequired().HasMaxLength(40).HasColumnType("TEXT");
            entity.Property<string>("Email").IsRequired().HasMaxLength(160).HasColumnType("TEXT");
            entity.Property<DateTime?>("GuncellenmeTarihi").HasColumnType("TEXT");
            entity.Property<int?>("GuncelleyenKullaniciId").HasColumnType("INTEGER");
            entity.Property<string>("OgrenciNumarasi").IsRequired().HasMaxLength(30).HasColumnType("TEXT");
            entity.Property<DateTime>("OlusturulmaTarihi").HasColumnType("TEXT");
            entity.Property<int?>("OlusturanKullaniciId").HasColumnType("INTEGER");
            entity.Property<double>("OrtalamaPuan").HasColumnType("REAL");
            entity.Property<string>("ProfilFotografiUrl").IsRequired().HasMaxLength(300).HasColumnType("TEXT");
            entity.Property<string>("SifreHash").IsRequired().HasMaxLength(512).HasColumnType("TEXT");
            entity.Property<bool>("SilindiMi").HasColumnType("INTEGER");
            entity.Property<string>("Soyad").IsRequired().HasMaxLength(60).HasColumnType("TEXT");
            entity.Property<string>("TelefonNumarasi").IsRequired().HasMaxLength(20).HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("Email").IsUnique();
            entity.ToTable("Kullanicilar");
        });

        modelBuilder.Entity("KampusRota.Api.Models.Yolculuk", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<string>("Aciklama").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<bool>("AktifMi").HasColumnType("INTEGER");
            entity.Property<int>("BosKoltukSayisi").HasColumnType("INTEGER");
            entity.Property<DateTime?>("GuncellenmeTarihi").HasColumnType("TEXT");
            entity.Property<int?>("GuncelleyenKullaniciId").HasColumnType("INTEGER");
            entity.Property<decimal>("KisiBasiUcret")
                .HasConversion<double>()
                .HasColumnType("REAL");
            entity.Property<string>("KalkisNoktasi").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            entity.Property<DateTime>("KalkisZamani").HasColumnType("TEXT");
            entity.Property<DateTime>("OlusturulmaTarihi").HasColumnType("TEXT");
            entity.Property<int?>("OlusturanKullaniciId").HasColumnType("INTEGER");
            entity.Property<bool>("SadeceKadinlarMi").HasColumnType("INTEGER");
            entity.Property<bool>("SilindiMi").HasColumnType("INTEGER");
            entity.Property<int>("SurucuId").HasColumnType("INTEGER");
            entity.Property<string>("VarisNoktasi").IsRequired().HasMaxLength(120).HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("SurucuId");
            entity.ToTable("Yolculuklar");
        });

        modelBuilder.Entity("KampusRota.Api.Models.Yolculuk", entity =>
        {
            entity.HasOne("KampusRota.Api.Models.Kullanici", "Surucu")
                .WithMany()
                .HasForeignKey("SurucuId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.Navigation("Surucu");
        });
    }
}
