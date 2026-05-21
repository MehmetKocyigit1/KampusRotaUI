using KampusRotaUI.Models;

namespace KampusRotaUI.Services;

public interface IApiService
{
    Task<Kullanici?> LoginAsync(string email, string sifre);
    Task<Kullanici?> KayitOlAsync(Kullanici yeniKullanici);
    Task<bool> SifreDegistirAsync(int kullaniciId, SifreDegistirmeIstegi istek);
    Task<bool> YolculukEkleAsync(Yolculuk yeniYolculuk, int kullaniciId);
    Task<bool> YolculukGuncelleAsync(Yolculuk yolculuk, int kullaniciId);
    Task<List<Yolculuk>> TumYolculuklariGetirAsync();
    Task<List<Yolculuk>> YolculuklariAraAsync(string? kalkis, string? varis, DateTime? tarih, bool? sadeceKadinlar);
    Task<bool> YolculukSilAsync(int yolculukId, int silenKullaniciId);
}
