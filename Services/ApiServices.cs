using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using KampusRotaUI.Models;

namespace KampusRotaUI.Services;

public class ApiServices
{
    private readonly HttpClient _httpClient;


    private string GetBaseUrl()
    {
        // Use HTTPS for Android emulator and rely on handler to ignore dev certificate errors
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "https://10.0.2.2:7107/";

        // For other platforms use localhost HTTPS
        return "https://localhost:7107/";
    }

    public ApiServices()
    {
        var handler = new HttpClientHandler();
        // In development ignore SSL certificate errors (keeps working on Windows and Android emulator when HTTPS is used)
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseUrl())
        };
    }
 

    // --- 1. KULLANICI İŞLEMLERİ (Backend API) ---

    public async Task<Kullanici?> LoginAsync(string email, string sifre)
    {
        try
        {
            var temizEmail = Uri.EscapeDataString(email);
            var temizSifre = Uri.EscapeDataString(sifre);

            var url = $"api/users/login?email={temizEmail}&sifre={temizSifre}";

            Debug.WriteLine($"[ApiServices] Login request URL: {_httpClient.BaseAddress}{url}");

            var response = await _httpClient.PostAsync(url, null);

            var respText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Giriş Başarısız. Durum Kodu: {response.StatusCode}. Response: {respText}");
                // Throw an exception with server response so UI can show it for debugging
                throw new Exception($"Server returned {(int)response.StatusCode} {response.ReasonPhrase}: {respText}");
            }

            // Successful response: try parse user
            try
            {
                var user = JsonSerializer.Deserialize<Kullanici>(respText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return user;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to deserialize Kullanici: " + ex);
                // As a fallback, attempt ReadFromJsonAsync
                return await response.Content.ReadFromJsonAsync<Kullanici>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("GİRİŞ HATASI: " + ex.ToString());
            // Rethrow so caller can show details
            throw;
        }
    }

    public async Task<Kullanici?> KayitOlAsync(Kullanici yeniKullanici)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/register", yeniKullanici);
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
            var response = await _httpClient.PostAsJsonAsync(url, yeniYolculuk);
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