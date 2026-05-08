using System.Diagnostics;
using System.Net.Http.Json;
using KampusRotaUI.Models;

namespace KampusRotaUI.Services;

public class ApiServices
{
    private readonly HttpClient _httpClient;

     private string GetBaseUrl()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
             return "https://10.0.2.2:7107/";
        }
        else if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Windows (PC) direkt localhost kullanır
            return "https://localhost:7107/";
        }

        return "https://localhost:7107/";
    }

    public ApiServices()
    {
        var handler = new HttpClientHandler();
        // Geliştirme (Dev) ortamında SSL sertifika hatalarını yok saymak için
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseUrl())
        };
    }

    // --- 1. KULLANICI İŞLEMLERİ ---

    public async Task<Kullanici?> GirisYapAsync(string email, string sifre)
    {
        try
        {
            var url = $"api/users/login?email={email}&sifre={sifre}";
            var response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                // Veriyi oku
                var kullanici = await response.Content.ReadFromJsonAsync<Kullanici>();

                // DİKKAT: Eğer JSON içinde Ad/Soyad gibi alanlar eksik geliyorsa 
                // ve biz bunlara UI tarafında erişiyorsak o meşhur null hatasını alırız.
                if (kullanici != null)
                {
                    // TamAd özelliği null ise hata vermemesi için kontrol ekliyoruz
                    Debug.WriteLine("Giriş Başarılı: " + (kullanici.TamAd ?? "İsimsiz Kullanıcı"));
                    return kullanici;
                }
            }

            Debug.WriteLine("Giriş başarısız: " + response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("BAĞLANTI HATASI: " + ex.Message);
            return null;
        }
    }

    public async Task<Kullanici?> KayitOlAsync(Kullanici yeniKullanici)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== KAYIT İŞLEMİ BAŞLADI ===");

            // Program.cs'deki Register ucuna (endpoint) istek atıyoruz
            var response = await _httpClient.PostAsJsonAsync("api/users/register", yeniKullanici);

            if (response.IsSuccessStatusCode)
            {
                var olusturulanKullanici = await response.Content.ReadFromJsonAsync<Kullanici>();
                return olusturulanKullanici;
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("KAYIT HATA: " + ex.Message);
            return null;
        }
    }

    public async Task<bool> SifreDegistirAsync(int kullaniciId, SifreDegistirmeIstegi istek)
    {
        try
        {
            Debug.WriteLine($"=== ŞİFRE DEĞİŞTİRME BAŞLADI (Kullanıcı ID: {kullaniciId}) ===");

            // Program.cs'deki MapPut ucuna uygun URL
            var url = $"api/users/{kullaniciId}/change-password";

            // SifreDegistirmeIstegi modeli Body (Gövde) olarak gönderiliyor
            var response = await _httpClient.PutAsJsonAsync(url, istek);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ŞİFRE DEĞİŞTİRME HATA: " + ex.Message);
            return false;
        }
    }



    // --- 2. YOLCULUK İŞLEMLERİ ---

    public async Task<bool> YolculukEkleAsync(Yolculuk yeniYolculuk, int kullaniciId)
    {
        try
        {
            Debug.WriteLine("=== YOLCULUK EKLENİYOR ===");

            // Program.cs'deki uç nokta: /api/rides?kullaniciId={id}
            var url = $"api/rides?kullaniciId={kullaniciId}";

            var response = await _httpClient.PostAsJsonAsync(url, yeniYolculuk);

            Debug.WriteLine("YOLCULUK EKLEME STATUS: " + response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("YOLCULUK EKLEME HATA: " + ex.Message);
            return false;
        }
    }

    public async Task<List<Yolculuk>> TumYolculuklariGetirAsync()
    {
        try
        {
            // Ana sayfada (MainPage) tüm aktif ilanları listelemek için kullanılacak
            return await _httpClient.GetFromJsonAsync<List<Yolculuk>>("api/rides") ?? new List<Yolculuk>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("YOLCULUKLARI GETİRME HATA: " + ex.Message);
            return new List<Yolculuk>();
        }
    }

    public async Task<bool> YolculukSilAsync(int yolculukId, int silenKullaniciId)
    {
        try
        {
            // Kullanıcının kendi ilanını iptal etmesi/silmesi (Soft Delete) için eklendi
            var url = $"api/rides/{yolculukId}?silenKullaniciId={silenKullaniciId}";
            var response = await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("YOLCULUK SİLME HATA: " + ex.Message);
            return false;
        }
    }
}