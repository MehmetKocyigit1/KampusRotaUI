using KampusRota.Api.Data;
using KampusRota.Api.DTOs;
using KampusRota.Api.Interfaces;
using KampusRota.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KampusRota.Api.Services;

public class UserService : IUserService
{
    private readonly KampusRotaDbContext _dbContext;

    public UserService(KampusRotaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<Kullanici>> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Ad) ||
            string.IsNullOrWhiteSpace(request.Soyad) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Sifre))
        {
            return ServiceResult<Kullanici>.Fail("Zorunlu alanlar bos birakilamaz.");
        }

        if (!email.EndsWith(".edu.tr", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<Kullanici>.Fail("Sadece .edu.tr uzantili e-posta kullanilabilir.");
        }

        if (request.Sifre.Length < 6)
        {
            return ServiceResult<Kullanici>.Fail("Sifre en az 6 karakter olmalidir.");
        }

        var emailKullaniliyorMu = await _dbContext.Kullanicilar
            .AnyAsync(k => k.Email == email && !k.SilindiMi);

        if (emailKullaniliyorMu)
        {
            return ServiceResult<Kullanici>.Fail("Bu e-posta zaten kullaniliyor.");
        }

        var kullanici = new Kullanici
        {
            Ad = request.Ad.Trim(),
            Soyad = request.Soyad.Trim(),
            Email = email,
            SifreHash = PasswordService.Hash(request.Sifre),
            TelefonNumarasi = request.TelefonNumarasi?.Trim() ?? string.Empty,
            OgrenciNumarasi = request.OgrenciNumarasi?.Trim() ?? string.Empty,
            Cinsiyet = request.Cinsiyet?.Trim() ?? string.Empty,
            OlusturulmaTarihi = DateTime.UtcNow,
            AktifMi = true,
            SilindiMi = false
        };

        _dbContext.Kullanicilar.Add(kullanici);
        await _dbContext.SaveChangesAsync();

        kullanici.OlusturanKullaniciId = kullanici.Id;
        await _dbContext.SaveChangesAsync();

        return ServiceResult<Kullanici>.Ok(ToSafeUser(kullanici), "Kayit basarili.");
    }

    public async Task<ServiceResult<Kullanici>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var kullanici = await _dbContext.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Email == email && k.AktifMi && !k.SilindiMi);

        if (kullanici is null || !PasswordService.Verify(request.Sifre, kullanici.SifreHash))
        {
            return ServiceResult<Kullanici>.Fail("E-posta veya sifre hatali.");
        }

        return ServiceResult<Kullanici>.Ok(ToSafeUser(kullanici), "Giris basarili.");
    }

    public async Task<ServiceResult<Kullanici>> GetByIdAsync(int id)
    {
        var kullanici = await _dbContext.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id && k.AktifMi && !k.SilindiMi);

        return kullanici is null
            ? ServiceResult<Kullanici>.Fail("Kullanici bulunamadi.")
            : ServiceResult<Kullanici>.Ok(ToSafeUser(kullanici));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(int kullaniciId, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EskiSifre) ||
            string.IsNullOrWhiteSpace(request.YeniSifre) ||
            string.IsNullOrWhiteSpace(request.YeniSifreTekrar))
        {
            return ServiceResult<bool>.Fail("Tum alanlar doldurulmalidir.");
        }

        if (request.YeniSifre.Length < 6)
        {
            return ServiceResult<bool>.Fail("Yeni sifre en az 6 karakter olmalidir.");
        }

        if (request.YeniSifre != request.YeniSifreTekrar)
        {
            return ServiceResult<bool>.Fail("Yeni sifreler eslesmiyor.");
        }

        var kullanici = await _dbContext.Kullanicilar
            .FirstOrDefaultAsync(k => k.Id == kullaniciId && k.AktifMi && !k.SilindiMi);

        if (kullanici is null || !PasswordService.Verify(request.EskiSifre, kullanici.SifreHash))
        {
            return ServiceResult<bool>.Fail("Mevcut sifre hatali.");
        }

        kullanici.SifreHash = PasswordService.Hash(request.YeniSifre);
        kullanici.GuncellenmeTarihi = DateTime.UtcNow;
        kullanici.GuncelleyenKullaniciId = kullaniciId;

        await _dbContext.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true, "Sifre guncellendi.");
    }

    private static Kullanici ToSafeUser(Kullanici kullanici)
        => new()
        {
            Id = kullanici.Id,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Email = kullanici.Email,
            SifreHash = string.Empty,
            TelefonNumarasi = kullanici.TelefonNumarasi,
            OgrenciNumarasi = kullanici.OgrenciNumarasi,
            ProfilFotografiUrl = kullanici.ProfilFotografiUrl,
            Cinsiyet = kullanici.Cinsiyet,
            OrtalamaPuan = kullanici.OrtalamaPuan,
            Biyografi = kullanici.Biyografi,
            OlusturulmaTarihi = kullanici.OlusturulmaTarihi,
            OlusturanKullaniciId = kullanici.OlusturanKullaniciId,
            GuncellenmeTarihi = kullanici.GuncellenmeTarihi,
            GuncelleyenKullaniciId = kullanici.GuncelleyenKullaniciId,
            AktifMi = kullanici.AktifMi,
            SilindiMi = kullanici.SilindiMi
        };
}
