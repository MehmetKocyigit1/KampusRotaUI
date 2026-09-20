using KampusRotaUI.Models;
using KampusRotaUI.Services;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Storage;

namespace KampusRotaUI.Views;

public partial class Login : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();
    private List<University> _allUniversities = new();

    public Login()
    {
        InitializeComponent();

        RememberMeCheck.CheckedChanged += OnRememberMeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadUniversitiesAsync();

        try
        {
            bool remember = Preferences.Default.Get("RememberMe", false);
            if (remember)
            {
                var savedEmail = Preferences.Default.Get("SavedEmail", string.Empty);
                var savedPassword = Preferences.Default.Get("SavedPassword", string.Empty);

                if (!string.IsNullOrEmpty(savedEmail))
                    EmailEntry.Text = savedEmail;
                if (!string.IsNullOrEmpty(savedPassword))
                    PasswordEntry.Text = savedPassword;

                RememberMeCheck.IsChecked = true;
            }
            else
            {
                RememberMeCheck.IsChecked = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error loading saved credentials: " + ex.Message);
        }
    }

    private async Task LoadUniversitiesAsync()
    {
        try
        {
            _allUniversities = await _apiService.GetUniversitiesAsync();
            if (_allUniversities == null || _allUniversities.Count == 0)
                return;

            var cities = _allUniversities
                .Select(u => u.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            CityPicker.Items.Clear();
            CityPicker.Items.Add("Tüm Şehirler");
            foreach (var city in cities)
            {
                CityPicker.Items.Add(city);
            }

            var savedUniId = Preferences.Default.Get("UserUniversityId", 0);
            var savedCity = Preferences.Default.Get("UserCity", string.Empty);

            if (!string.IsNullOrEmpty(savedCity) && CityPicker.Items.Contains(savedCity))
            {
                CityPicker.SelectedItem = savedCity;
            }
            else
            {
                CityPicker.SelectedIndex = 0;
            }

            if (savedUniId > 0)
            {
                var savedUni = _allUniversities.FirstOrDefault(u => u.Id == savedUniId);
                if (savedUni != null)
                {
                    UniversityPicker.SelectedItem = savedUni;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login universities load error: {ex.Message}");
        }
    }

    private void OnCitySelectedIndexChanged(object? sender, EventArgs e)
    {
        if (CityPicker.SelectedIndex < 0) return;

        var selectedCity = CityPicker.SelectedItem?.ToString();
        List<University> filtered;

        if (string.IsNullOrEmpty(selectedCity) || selectedCity == "Tüm Şehirler")
        {
            filtered = _allUniversities;
        }
        else
        {
            filtered = _allUniversities
                .Where(u => string.Equals(u.City, selectedCity, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        UniversityPicker.ItemsSource = null;
        UniversityPicker.ItemsSource = filtered;

        if (filtered.Count > 0)
        {
            UniversityPicker.SelectedIndex = 0;
        }
    }

    private void OnUniversitySelectedIndexChanged(object? sender, EventArgs e)
    {
        if (UniversityPicker.SelectedItem is University uni)
        {
            Preferences.Default.Set("UserUniversityId", uni.Id);
            Preferences.Default.Set("UserUniversityName", uni.Name);
            Preferences.Default.Set("UserCity", uni.City);
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (ErrorLabel != null) ErrorLabel.IsVisible = false;
        var loginButton = (Button)sender;
        loginButton.IsEnabled = false;

        var email = EmailEntry.Text?.Trim();
        var sifre = PasswordEntry.Text;

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre))
            {
                ShowError("Lütfen tüm alanları doldurun.");
                return;
            }

            if (!email.Contains('@') || email.Length > 100)
            {
                ShowError("Lütfen geçerli bir e-posta adresi girin.");
                return;
            }

            if (sifre.Length < 6)
            {
                ShowError("Şifre en az 6 karakter olmalıdır.");
                return;
            }

            var kullanici = await _apiService.LoginAsync(email, sifre);

            if (kullanici == null)
            {
                ShowError("Giriş yapılamadı. Bilgilerinizi kontrol edin veya hesabınızın aktif olduğundan emin olun.");
                return;
            }

            var userId = kullanici.Id.ToString();
            var tamAd = kullanici.TamAd ?? "Kullanıcı";
            var userEmail = kullanici.Email ?? email;

            Preferences.Default.Set("UserId", userId);
            Preferences.Default.Set("UserFullName", tamAd);
            Preferences.Default.Set("UserEmail", userEmail);
            Preferences.Default.Set("UserGender", kullanici.Cinsiyet);
            Preferences.Default.Set("UserPhone", kullanici.TelefonNumarasi ?? string.Empty);
            Preferences.Default.Set("UserStudentNo", kullanici.OgrenciNumarasi ?? string.Empty);
            Preferences.Default.Set("UserBio", kullanici.Biyografi ?? string.Empty);
            Preferences.Default.Set("UserProfilePhotoUrl", kullanici.ProfilFotografiUrl ?? string.Empty);
            Preferences.Default.Set("UserRating", kullanici.OrtalamaPuan.ToString("0.0", CultureInfo.InvariantCulture));

            var selectedUni = UniversityPicker.SelectedItem as University;
            if (selectedUni != null)
            {
                Preferences.Default.Set("UserUniversityId", selectedUni.Id);
                Preferences.Default.Set("UserUniversityName", selectedUni.Name);
                Preferences.Default.Set("UserCity", selectedUni.City);
            }
            else if (kullanici.UniversityId.HasValue)
            {
                Preferences.Default.Set("UserUniversityId", kullanici.UniversityId.Value);
                if (!string.IsNullOrEmpty(kullanici.UniversityName))
                {
                    Preferences.Default.Set("UserUniversityName", kullanici.UniversityName);
                }
                if (!string.IsNullOrEmpty(kullanici.City))
                {
                    Preferences.Default.Set("UserCity", kullanici.City);
                }
            }

            if (RememberMeCheck.IsChecked)
            {
                Preferences.Default.Set("RememberMe", true);
                Preferences.Default.Set("SavedEmail", email ?? string.Empty);
                Preferences.Default.Set("SavedPassword", sifre ?? string.Empty);
            }
            else
            {
                Preferences.Default.Set("RememberMe", false);
                Preferences.Default.Remove("SavedEmail");
                Preferences.Default.Remove("SavedPassword");
            }

            Debug.WriteLine($"GİRİŞ BAŞARILI: {tamAd}");

            if (Application.Current is not null)
            {
                Application.Current.MainPage = new AppShell();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Login] Hata Detayı: {ex}");
            ShowError("Bir hata oluştu: " + ex.Message);
        }
        finally
        {
            if (loginButton != null) loginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnRegisterNavClicked(object sender, EventArgs e)
    {
        // Kayıt sayfasına geçiş yapıyoruz
        await Navigation.PushAsync(new RegisterPage());
    }

    private void OnRememberMeChanged(object? sender, CheckedChangedEventArgs e)
    {
        try
        {
            if (!e.Value)
            {
                Preferences.Default.Set("RememberMe", false);
                Preferences.Default.Remove("SavedEmail");
                Preferences.Default.Remove("SavedPassword");
            }
            else
            {
                Preferences.Default.Set("RememberMe", true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error handling RememberMe change: " + ex.Message);
        }
    }
}
