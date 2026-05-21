using KampusRota.Api.Data;
using KampusRota.Api.DTOs;
using KampusRota.Api.Interfaces;
using KampusRota.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KampusRota.Api.Services;

public class RideService : IRideService
{
    private readonly KampusRotaDbContext _dbContext;

    public RideService(KampusRotaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Yolculuk>> GetAllAsync(string? kalkis, string? varis, DateTime? tarih, bool? sadeceKadinlar)
    {
        var query = _dbContext.Yolculuklar
            .Include(y => y.Surucu)
            .AsNoTracking()
            .Where(y => y.AktifMi && !y.SilindiMi && y.KalkisZamani >= DateTime.Today)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(kalkis))
        {
            query = query.Where(y => y.KalkisNoktasi == kalkis);
        }

        if (!string.IsNullOrWhiteSpace(varis))
        {
            query = query.Where(y => y.VarisNoktasi == varis);
        }

        if (tarih.HasValue)
        {
            query = query.Where(y => y.KalkisZamani.Date == tarih.Value.Date);
        }

        if (sadeceKadinlar.HasValue)
        {
            query = query.Where(y => y.SadeceKadinlarMi == sadeceKadinlar.Value);
        }

        var yolculuklar = await query
            .OrderBy(y => y.KalkisZamani)
            .ThenBy(y => y.KalkisNoktasi)
            .ToListAsync();

        return yolculuklar.Select(ToSafeRide).ToList();
    }

    public async Task<IReadOnlyList<Yolculuk>> GetByUserAsync(int kullaniciId)
    {
        var yolculuklar = await _dbContext.Yolculuklar
            .Include(y => y.Surucu)
            .AsNoTracking()
            .Where(y => y.SurucuId == kullaniciId && !y.SilindiMi)
            .OrderByDescending(y => y.KalkisZamani)
            .ToListAsync();

        return yolculuklar.Select(ToSafeRide).ToList();
    }

    public async Task<ServiceResult<Yolculuk>> GetByIdAsync(int id)
    {
        var yolculuk = await _dbContext.Yolculuklar
            .Include(y => y.Surucu)
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == id && !y.SilindiMi);

        return yolculuk is null
            ? ServiceResult<Yolculuk>.Fail("Yolculuk bulunamadi.")
            : ServiceResult<Yolculuk>.Ok(ToSafeRide(yolculuk));
    }

    public async Task<ServiceResult<Yolculuk>> CreateAsync(int kullaniciId, RideCreateRequest request)
    {
        var validation = ValidateRide(request.KalkisNoktasi, request.VarisNoktasi, request.KalkisZamani, request.BosKoltukSayisi, request.KisiBasiUcret);
        if (validation is not null)
        {
            return ServiceResult<Yolculuk>.Fail(validation);
        }

        var surucuVarMi = await _dbContext.Kullanicilar
            .AnyAsync(k => k.Id == kullaniciId && k.AktifMi && !k.SilindiMi);

        if (!surucuVarMi)
        {
            return ServiceResult<Yolculuk>.Fail("Aktif kullanici bulunamadi.");
        }

        var yolculuk = new Yolculuk
        {
            SurucuId = kullaniciId,
            KalkisNoktasi = request.KalkisNoktasi.Trim(),
            VarisNoktasi = request.VarisNoktasi.Trim(),
            KalkisZamani = request.KalkisZamani,
            BosKoltukSayisi = request.BosKoltukSayisi,
            KisiBasiUcret = request.KisiBasiUcret,
            Aciklama = request.Aciklama?.Trim() ?? string.Empty,
            SadeceKadinlarMi = request.SadeceKadinlarMi,
            OlusturanKullaniciId = kullaniciId,
            OlusturulmaTarihi = DateTime.UtcNow,
            AktifMi = true,
            SilindiMi = false
        };

        _dbContext.Yolculuklar.Add(yolculuk);
        await _dbContext.SaveChangesAsync();

        return ServiceResult<Yolculuk>.Ok(ToSafeRide(yolculuk), "Yolculuk eklendi.");
    }

    public async Task<ServiceResult<Yolculuk>> UpdateAsync(int id, int guncelleyenKullaniciId, RideUpdateRequest request)
    {
        var validation = ValidateRide(request.KalkisNoktasi, request.VarisNoktasi, request.KalkisZamani, request.BosKoltukSayisi, request.KisiBasiUcret);
        if (validation is not null)
        {
            return ServiceResult<Yolculuk>.Fail(validation);
        }

        var yolculuk = await _dbContext.Yolculuklar
            .FirstOrDefaultAsync(y => y.Id == id && !y.SilindiMi);

        if (yolculuk is null)
        {
            return ServiceResult<Yolculuk>.Fail("Yolculuk bulunamadi.");
        }

        if (yolculuk.SurucuId != guncelleyenKullaniciId)
        {
            return ServiceResult<Yolculuk>.Fail("Sadece ilani olusturan kullanici guncelleyebilir.");
        }

        yolculuk.KalkisNoktasi = request.KalkisNoktasi.Trim();
        yolculuk.VarisNoktasi = request.VarisNoktasi.Trim();
        yolculuk.KalkisZamani = request.KalkisZamani;
        yolculuk.BosKoltukSayisi = request.BosKoltukSayisi;
        yolculuk.KisiBasiUcret = request.KisiBasiUcret;
        yolculuk.Aciklama = request.Aciklama?.Trim() ?? string.Empty;
        yolculuk.SadeceKadinlarMi = request.SadeceKadinlarMi;
        yolculuk.AktifMi = request.AktifMi;
        yolculuk.GuncelleyenKullaniciId = guncelleyenKullaniciId;
        yolculuk.GuncellenmeTarihi = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return ServiceResult<Yolculuk>.Ok(ToSafeRide(yolculuk), "Yolculuk guncellendi.");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int silenKullaniciId)
    {
        var yolculuk = await _dbContext.Yolculuklar
            .FirstOrDefaultAsync(y => y.Id == id && !y.SilindiMi);

        if (yolculuk is null)
        {
            return ServiceResult<bool>.Fail("Yolculuk bulunamadi.");
        }

        if (yolculuk.SurucuId != silenKullaniciId)
        {
            return ServiceResult<bool>.Fail("Sadece ilani olusturan kullanici silebilir.");
        }

        yolculuk.SilindiMi = true;
        yolculuk.AktifMi = false;
        yolculuk.GuncelleyenKullaniciId = silenKullaniciId;
        yolculuk.GuncellenmeTarihi = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Yolculuk silindi.");
    }

    private static string? ValidateRide(string kalkis, string varis, DateTime kalkisZamani, int bosKoltukSayisi, decimal kisiBasiUcret)
    {
        if (string.IsNullOrWhiteSpace(kalkis) || string.IsNullOrWhiteSpace(varis))
        {
            return "Kalkis ve varis noktalari zorunludur.";
        }

        if (string.Equals(kalkis.Trim(), varis.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "Kalkis ve varis noktasi ayni olamaz.";
        }

        if (kalkisZamani < DateTime.Now.AddMinutes(-5))
        {
            return "Gecmis tarihli yolculuk eklenemez.";
        }

        if (bosKoltukSayisi is < 1 or > 8)
        {
            return "Bos koltuk sayisi 1 ile 8 arasinda olmalidir.";
        }

        if (kisiBasiUcret < 0)
        {
            return "Ucret negatif olamaz.";
        }

        return null;
    }

    private static Yolculuk ToSafeRide(Yolculuk yolculuk)
        => new()
        {
            Id = yolculuk.Id,
            SurucuId = yolculuk.SurucuId,
            Surucu = yolculuk.Surucu is null
                ? null
                : new Kullanici
                {
                    Id = yolculuk.Surucu.Id,
                    Ad = yolculuk.Surucu.Ad,
                    Soyad = yolculuk.Surucu.Soyad,
                    Email = yolculuk.Surucu.Email,
                    SifreHash = string.Empty,
                    TelefonNumarasi = yolculuk.Surucu.TelefonNumarasi,
                    OgrenciNumarasi = yolculuk.Surucu.OgrenciNumarasi,
                    ProfilFotografiUrl = yolculuk.Surucu.ProfilFotografiUrl,
                    Cinsiyet = yolculuk.Surucu.Cinsiyet,
                    OrtalamaPuan = yolculuk.Surucu.OrtalamaPuan,
                    Biyografi = yolculuk.Surucu.Biyografi,
                    AktifMi = yolculuk.Surucu.AktifMi,
                    SilindiMi = yolculuk.Surucu.SilindiMi,
                    OlusturulmaTarihi = yolculuk.Surucu.OlusturulmaTarihi,
                    OlusturanKullaniciId = yolculuk.Surucu.OlusturanKullaniciId,
                    GuncellenmeTarihi = yolculuk.Surucu.GuncellenmeTarihi,
                    GuncelleyenKullaniciId = yolculuk.Surucu.GuncelleyenKullaniciId
                },
            KalkisNoktasi = yolculuk.KalkisNoktasi,
            VarisNoktasi = yolculuk.VarisNoktasi,
            KalkisZamani = yolculuk.KalkisZamani,
            BosKoltukSayisi = yolculuk.BosKoltukSayisi,
            KisiBasiUcret = yolculuk.KisiBasiUcret,
            Aciklama = yolculuk.Aciklama,
            SadeceKadinlarMi = yolculuk.SadeceKadinlarMi,
            AktifMi = yolculuk.AktifMi,
            SilindiMi = yolculuk.SilindiMi,
            OlusturulmaTarihi = yolculuk.OlusturulmaTarihi,
            OlusturanKullaniciId = yolculuk.OlusturanKullaniciId,
            GuncellenmeTarihi = yolculuk.GuncellenmeTarihi,
            GuncelleyenKullaniciId = yolculuk.GuncelleyenKullaniciId
        };
}
