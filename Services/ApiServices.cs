using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KampusRotaUI.Models;

namespace KampusRotaUI.Services;

public class ApiServices : IApiService
{
    private readonly HttpClient _httpClient;

    private readonly string _openRouterKey = "Akif attım sana wp den";
    private readonly string _openRouterUrl = "https://openrouter.ai/api/v1/chat/completions";

    private string GetBaseUrl()
    {
        // Android emülatörün localhost'a erişebilmesi için 10.0.2.2 kullanıyoruz
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "https://10.0.2.2:7107/";

        return "https://localhost:7107/";
    }

    public ApiServices()
    {
        var handler = new HttpClientHandler();
        // Geliştirme aşamasında SSL sertifika hatalarını görmezden geliyoruz
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseUrl())
        };
    }

    // --- ANTIGRAVITY (OpenRouter) METODU ---
    // Bu metot AGENT.md dosyasını okur ve Gemini'ye gönderir
    public async Task<string> AskGeminiAsync(string userPrompt)
    {
        try
        {
            // 1. AGENT.md dosyasını uygulama paketinden okuyoruz
            string agentInstructions = "";
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("AGENT.md");
                using var reader = new StreamReader(stream);
                agentInstructions = await reader.ReadToEndAsync();
            }
            catch
            {
                agentInstructions = "Sen KampusRota uygulamasının asistanı Antigravity'sin.";
            }

            // 2. OpenRouter için JSON gövdesini (Payload) hazırlıyoruz
            var requestBody = new
            {
                model = "google/gemini-2.0-flash-001",
                messages = new[]
                {
                    new { role = "system", content = agentInstructions },
                    new { role = "user", content = userPrompt }
                }
            };

            // 3. HTTP İsteğini manuel oluşturuyoruz (Flutter mantığı ile aynı)
            using var request = new HttpRequestMessage(HttpMethod.Post, _openRouterUrl);
            request.Headers.Add("Authorization", $"Bearer {_openRouterKey}");
            request.Headers.Add("HTTP-Referer", "https://kampusrota.com"); // OpenRouter için tavsiye edilir

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 4. İsteği gönderiyoruz
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            if (response.IsSuccessStatusCode)
            {
                

                // OpenRouter JSON yapısından asıl cevabı çekiyoruz: choices[0].message.content
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Cevap boş döndü.";
            }

            return $"OPENROUTER HATASI: {response.StatusCode} - {responseContent}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ANTIGRAVITY HATA: " + ex.Message);
            return "Şu an cevap veremiyorum, lütfen internet bağlantınızı kontrol edin.";
        }
    }

    // --- 1. KULLANICI İŞLEMLERİ (Backend API) ---

    public async Task<Kullanici?> LoginAsync(string email, string sifre)
    {
        try
        {
            var url = $"api/users/login?email={Uri.EscapeDataString(email)}&sifre={Uri.EscapeDataString(sifre)}";
            var response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Kullanici>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("GİRİŞ HATASI: " + ex.Message);
            return null;
        }
    }

    public async Task<Kullanici?> KayitOlAsync(Kullanici yeniKullanici)
    {
        try
        {
            var request = new
            {
                yeniKullanici.Ad,
                yeniKullanici.Soyad,
                yeniKullanici.Email,
                Sifre = yeniKullanici.SifreHash,
                yeniKullanici.TelefonNumarasi,
                yeniKullanici.OgrenciNumarasi,
                yeniKullanici.Cinsiyet
            };

            var response = await _httpClient.PostAsJsonAsync("api/users/register", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Kullanici>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("KAYIT HATASI: " + ex.Message);
            return null;
        }
    }

    public async Task<bool> SifreDegistirAsync(int kullaniciId, SifreDegistirmeIstegi istek)
    {
        try
        {
            var url = $"api/users/{kullaniciId}/change-password";
            var response = await _httpClient.PutAsJsonAsync(url, istek);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ŞİFRE GÜNCELLEME HATASI: " + ex.Message);
            return false;
        }
    }

    // --- 2. YOLCULUK İŞLEMLERİ ---

    public async Task<bool> YolculukEkleAsync(Yolculuk yeniYolculuk, int kullaniciId)
    {
        try
        {
            var url = $"api/rides?kullaniciId={kullaniciId}";
            var request = new
            {
                yeniYolculuk.KalkisNoktasi,
                yeniYolculuk.VarisNoktasi,
                yeniYolculuk.KalkisZamani,
                yeniYolculuk.BosKoltukSayisi,
                yeniYolculuk.KisiBasiUcret,
                yeniYolculuk.Aciklama,
                yeniYolculuk.SadeceKadinlarMi
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("YOLCULUK EKLEME HATASI: " + ex.Message);
            return false;
        }
    }

    public async Task<List<Yolculuk>> TumYolculuklariGetirAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Yolculuk>>("api/rides") ?? new List<Yolculuk>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("VERİ ÇEKME HATASI: " + ex.Message);
            return new List<Yolculuk>();
        }
    }

    public async Task<List<Yolculuk>> YolculuklariAraAsync(string? kalkis, string? varis, DateTime? tarih, bool? sadeceKadinlar)
    {
        try
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(kalkis))
                query.Add($"kalkis={Uri.EscapeDataString(kalkis)}");

            if (!string.IsNullOrWhiteSpace(varis))
                query.Add($"varis={Uri.EscapeDataString(varis)}");

            if (tarih.HasValue)
                query.Add($"tarih={Uri.EscapeDataString(tarih.Value.ToString("yyyy-MM-dd"))}");

            if (sadeceKadinlar.HasValue)
                query.Add($"sadeceKadinlar={sadeceKadinlar.Value.ToString().ToLowerInvariant()}");

            var url = query.Count == 0 ? "api/rides" : $"api/rides?{string.Join("&", query)}";
            return await _httpClient.GetFromJsonAsync<List<Yolculuk>>(url) ?? new List<Yolculuk>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ARAMA HATASI: " + ex.Message);
            return new List<Yolculuk>();
        }
    }

    public async Task<bool> YolculukGuncelleAsync(Yolculuk yolculuk, int kullaniciId)
    {
        try
        {
            var url = $"api/rides/{yolculuk.Id}?guncelleyenKullaniciId={kullaniciId}";
            var request = new
            {
                yolculuk.KalkisNoktasi,
                yolculuk.VarisNoktasi,
                yolculuk.KalkisZamani,
                yolculuk.BosKoltukSayisi,
                yolculuk.KisiBasiUcret,
                yolculuk.Aciklama,
                yolculuk.SadeceKadinlarMi,
                yolculuk.AktifMi
            };

            var response = await _httpClient.PutAsJsonAsync(url, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("GUNCELLEME HATASI: " + ex.Message);
            return false;
        }
    }

    public async Task<bool> YolculukSilAsync(int yolculukId, int silenKullaniciId)
    {
        try
        {
            var url = $"api/rides/{yolculukId}?silenKullaniciId={silenKullaniciId}";
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("SİLME HATASI: " + ex.Message);
            return false;
        }
    }
}
