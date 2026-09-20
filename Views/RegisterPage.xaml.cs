using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using KampusRotaUI.Models;
using KampusRotaUI.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace KampusRotaUI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();
    private List<University> _allUniversities = new();
    private bool _isUpdatingEmailProgrammatically = false;
    private string _selectedGender = "Erkek";

    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUniversitiesAsync();
    }

    private async Task LoadUniversitiesAsync()
    {
        try
        {
            var unis = await _apiService.GetUniversitiesAsync();
            if (unis != null && unis.Count > 0)
            {
                _allUniversities = unis;
                var cities = _allUniversities
                    .Select(u => u.City)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                CityPicker.ItemsSource = cities;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Register] Üniversiteler yüklenemedi: {ex.Message}");
        }
    }

    private void OnCitySelectedIndexChanged(object sender, EventArgs e)
    {
        if (CityPicker.SelectedItem is string selectedCity)
        {
            var filteredUnis = _allUniversities
                .Where(u => u.City.Equals(selectedCity, StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.Name)
                .ToList();

            UniversityPicker.ItemsSource = filteredUnis;
            if (filteredUnis.Count == 1)
            {
                UniversityPicker.SelectedItem = filteredUnis[0];
            }
        }
    }

    private void OnUniversitySelectedIndexChanged(object sender, EventArgs e)
    {
        if (UniversityPicker.SelectedItem is University selectedUni)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                EmailEntry.Placeholder = $"ornek@{selectedUni.EmailDomain}";
            }
        }
    }

    private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingEmailProgrammatically) return;

        var email = e.NewTextValue?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || !email.Contains('@')) return;

        var domain = email.Split('@').Last();
        if (string.IsNullOrWhiteSpace(domain)) return;

        var matchedUni = _allUniversities.FirstOrDefault(u =>
            u.EmailDomain.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            domain.EndsWith("." + u.EmailDomain, StringComparison.OrdinalIgnoreCase) ||
            u.EmailDomain.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

        if (matchedUni != null)
        {
            _isUpdatingEmailProgrammatically = true;
            try
            {
                if (CityPicker.SelectedItem as string != matchedUni.City)
                {
                    CityPicker.SelectedItem = matchedUni.City;
                }

                UniversityPicker.SelectedItem = matchedUni;
            }
            finally
            {
                _isUpdatingEmailProgrammatically = false;
            }
        }
    }

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
        var telefon = PhoneEntry.Text?.Trim() ?? string.Empty;
        var sifre = PasswordEntry.Text;
        var sifreTekrar = ConfirmPasswordEntry.Text;
        var cinsiyet = _selectedGender;
        var selectedUni = UniversityPicker.SelectedItem as University;

        try
        {
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

            if (selectedUni == null)
            {
                await DisplayAlert(
                    "Üniversite Seçimi",
                    "Lütfen okuduğunuz şehir ve üniversiteyi seçin.",
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

            if (telefon.Length > 20)
            {
                await DisplayAlert(
                    "Telefon",
                    "Telefon numarası en fazla 20 karakter olabilir.",
                    "Tamam");
                return;
            }

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
                TelefonNumarasi = telefon,
                SifreHash = sifre,
                Cinsiyet = cinsiyet,
                UniversityId = selectedUni.Id,
                AktifMi = true,
                SilindiMi = false
            };

            // =========================
            // API İSTEĞİ
            // =========================

            var kayitliKullanici = await _apiService.KayitOlAsync(yeniKullanici);

            if (kayitliKullanici != null)
            {
                Preferences.Default.Set("UserUniversityId", selectedUni.Id);
                Preferences.Default.Set("UserUniversityName", selectedUni.Name);
                Preferences.Default.Set("UserCity", selectedUni.City);

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
            System.Diagnostics.Debug.WriteLine($"[Register] Hata: {ex.Message}");
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
