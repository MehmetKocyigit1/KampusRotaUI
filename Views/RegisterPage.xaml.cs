using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly IApiService _apiService = new ApiServices();

    // Seçilen cinsiyet burada tutulacak
    private string selectedGender = "Erkek";

    public RegisterPage()
    {
        InitializeComponent();
    }

    // =========================
    // CİNSİYET SEÇİMİ
    // =========================

    private void ResetGenderBorders()
    {
        MaleBorder.BackgroundColor = Color.FromArgb("#0A1A4F");
        FemaleBorder.BackgroundColor = Color.FromArgb("#0A1A4F");
        UnknownBorder.BackgroundColor = Color.FromArgb("#0A1A4F");
    }

    private void OnMaleTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        MaleBorder.BackgroundColor = Color.FromArgb("#FFB300");

        selectedGender = "Erkek";
    }

    private void OnFemaleTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        FemaleBorder.BackgroundColor = Color.FromArgb("#FFB300");

        selectedGender = "Kadın";
    }

    private void OnUnknownTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        UnknownBorder.BackgroundColor = Color.FromArgb("#FFB300");

        selectedGender = "Belirtmek İstemiyorum";
    }

    // =========================
    // KAYIT OL
    // =========================

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var registerButton = (Button)sender;
        registerButton.IsEnabled = false;

        string ad = NameEntry.Text?.Trim();
        string soyad = SurnameEntry.Text?.Trim();
        string ogrenciNo = StudentNoEntry.Text?.Trim();
        string email = EmailEntry.Text?.Trim();
        string sifre = PasswordEntry.Text;
        string sifreTekrar = ConfirmPasswordEntry.Text;

        // Picker kaldırıldığı için artık buradan geliyor
        string cinsiyet = selectedGender;

        try
        {
            // =========================
            // BOŞ ALAN KONTROLÜ
            // =========================

            if (string.IsNullOrWhiteSpace(ad) ||
                string.IsNullOrWhiteSpace(soyad) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(sifre) ||
                string.IsNullOrWhiteSpace(cinsiyet))
            {
                await DisplayAlert(
                    "Eksik Bilgi",
                    "Lütfen tüm zorunlu alanları doldurun.",
                    "Tamam");

                return;
            }

            // =========================
            // E-POSTA KONTROLÜ
            // =========================

            if (!email.Contains('@') || !email.EndsWith(".edu.tr", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Geçersiz E-posta",
                    "Sadece üniversite uzantılı (.edu.tr) e-posta kullanılabilir.",
                    "Tamam");

                return;
            }

            // =========================
            // ŞİFRE KONTROLLERİ
            // =========================

            if (sifre.Length < 6)
            {
                await DisplayAlert(
                    "Güvenlik",
                    "Şifreniz en az 6 karakter olmalıdır.",
                    "Tamam");

                return;
            }

            if (sifre != sifreTekrar)
            {
                await DisplayAlert(
                    "Hata",
                    "Şifreler eşleşmiyor.",
                    "Tamam");

                return;
            }

            // =========================
            // KULLANICI OLUŞTUR
            // =========================

            var yeniKullanici = new Kullanici
            {
                Ad = ad,
                Soyad = soyad,
                OgrenciNumarasi = ogrenciNo ?? "",
                Email = email,
                SifreHash = sifre,
                TelefonNumarasi = string.Empty,
                Cinsiyet = cinsiyet,
                AktifMi = true,
                SilindiMi = false
            };

            // =========================
            // API İSTEĞİ
            // =========================

            var kayitliKullanici =
                await _apiService.KayitOlAsync(yeniKullanici);

            if (kayitliKullanici != null)
            {
                await DisplayAlert(
                    "Başarılı!",
                    "Hesabınız başarıyla oluşturuldu.",
                    "Harika");

                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert(
                    "Hata",
                    "Bu e-posta zaten kullanımda olabilir.",
                    "Tamam");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Register] Hata: {ex.Message}");

            await DisplayAlert(
                "Bağlantı Hatası",
                "Sunucuya ulaşılamıyor.",
                "Tamam");
        }
        finally
        {
            registerButton.IsEnabled = true;
        }
    }

    // =========================
    // GİRİŞ SAYFASI
    // =========================

    private async void OnLoginNavClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
