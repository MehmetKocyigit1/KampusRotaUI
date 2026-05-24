using KampusRotaUI.Models;
using KampusRotaUI.Services;
using System.Net.Mail;

namespace KampusRotaUI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();

    private string _selectedGender = "Erkek";

    public RegisterPage()
    {
        InitializeComponent();
    }

    // =========================
    // CİNSİYET SEÇİMİ
    // =========================

    private void ResetGenderBorders()
    {
        MaleBorder.BackgroundColor = Color.FromArgb("#3A4756");
        FemaleBorder.BackgroundColor = Color.FromArgb("#3A4756");
        UnknownBorder.BackgroundColor = Color.FromArgb("#3A4756");
    }

    private void OnMaleTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        MaleBorder.BackgroundColor = Color.FromArgb("#FFB300");

        _selectedGender = "Erkek";
    }

    private void OnFemaleTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        FemaleBorder.BackgroundColor = Color.FromArgb("#FFB300");

        _selectedGender = "Kadın";
    }

    private void OnUnknownTapped(object sender, TappedEventArgs e)
    {
        ResetGenderBorders();

        UnknownBorder.BackgroundColor = Color.FromArgb("#FFB300");

        _selectedGender = "Belirtmek İstemiyorum";
    }

    // =========================
    // KAYIT OL
    // =========================

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var registerButton = (Button)sender;
        registerButton.IsEnabled = false;

        var ad = NameEntry.Text?.Trim();
        var soyad = SurnameEntry.Text?.Trim();
        var ogrenciNo = StudentNoEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var sifre = PasswordEntry.Text;
        var sifreTekrar = ConfirmPasswordEntry.Text;
        var cinsiyet = _selectedGender;

        try
        {
            // =========================
            // BOŞ ALAN KONTROLÜ
            // =========================

            if (string.IsNullOrWhiteSpace(ad) ||
                string.IsNullOrWhiteSpace(soyad) ||
                string.IsNullOrWhiteSpace(ogrenciNo) ||
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

            if (ad.Length < 2 || ad.Length > 40 || soyad.Length < 2 || soyad.Length > 40)
            {
                await DisplayAlert(
                    "Geçersiz Bilgi",
                    "İsim ve soyisim 2-40 karakter arasında olmalıdır.",
                    "Tamam");

                return;
            }

            if (ogrenciNo.Length < 5 || ogrenciNo.Length > 20 || !ogrenciNo.All(char.IsDigit))
            {
                await DisplayAlert(
                    "Öğrenci No",
                    "Öğrenci numarası 5-20 haneli ve sadece rakamlardan oluşmalıdır.",
                    "Tamam");

                return;
            }

            // =========================
            // E-POSTA KONTROLÜ
            // =========================

            if (!IsValidUniversityEmail(email))
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

    private static bool IsValidUniversityEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address.Equals(email, StringComparison.OrdinalIgnoreCase)
                && address.Host.EndsWith(".edu.tr", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
