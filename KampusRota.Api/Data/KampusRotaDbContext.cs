using KampusRota.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KampusRota.Api.Data;

public class KampusRotaDbContext : DbContext
{
    public KampusRotaDbContext(DbContextOptions<KampusRotaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Yolculuk> Yolculuklar => Set<Yolculuk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasIndex(k => k.Email).IsUnique();
            entity.Property(k => k.Ad).HasMaxLength(60).IsRequired();
            entity.Property(k => k.Soyad).HasMaxLength(60).IsRequired();
            entity.Property(k => k.Email).HasMaxLength(160).IsRequired();
            entity.Property(k => k.SifreHash).HasMaxLength(512).IsRequired();
            entity.Property(k => k.TelefonNumarasi).HasMaxLength(20);
            entity.Property(k => k.OgrenciNumarasi).HasMaxLength(30);
            entity.Property(k => k.ProfilFotografiUrl).HasMaxLength(300);
            entity.Property(k => k.Cinsiyet).HasMaxLength(40);
            entity.Property(k => k.Biyografi).HasMaxLength(500);
        });

        modelBuilder.Entity<Yolculuk>(entity =>
        {
            entity.Property(y => y.KalkisNoktasi).HasMaxLength(120).IsRequired();
            entity.Property(y => y.VarisNoktasi).HasMaxLength(120).IsRequired();
            entity.Property(y => y.Aciklama).HasMaxLength(500);
            entity.Property(y => y.KisiBasiUcret).HasConversion<double>();

            entity.HasOne(y => y.Surucu)
                .WithMany()
                .HasForeignKey(y => y.SurucuId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
