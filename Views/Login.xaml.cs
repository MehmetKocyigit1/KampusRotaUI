using KampusRotaUI.Services;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Storage;

namespace KampusRotaUI.Views;

public partial class Login : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();

    public Login()
    {
        InitializeComponent();

        RememberMeCheck.CheckedChanged += OnRememberMeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

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

            if (kullanici.UniversityId.HasValue)
            {
                Preferences.Default.Set("UserUniversityId", kullanici.UniversityId.Value);
            }
            if (!string.IsNullOrEmpty(kullanici.UniversityName))
            {
                Preferences.Default.Set("UserUniversityName", kullanici.UniversityName);
            }
            if (!string.IsNullOrEmpty(kullanici.City))
            {
                Preferences.Default.Set("UserCity", kullanici.City);
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
