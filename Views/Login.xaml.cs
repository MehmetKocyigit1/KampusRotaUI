using KampusRotaUI.Services;
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace KampusRotaUI.Views;

public partial class Login : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();

    public Login()
    {
        InitializeComponent();
        Debug.WriteLine("LOGIN SAYFASI AÇILDI");

        // Attach checkbox handler after InitializeComponent so RememberMeCheck is available
        RememberMeCheck.CheckedChanged += OnRememberMeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load saved credentials if user previously chose "Remember me"
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

        string email = EmailEntry?.Text?.Trim();
        string sifre = PasswordEntry?.Text;

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre))
            {
                ShowError("Lütfen tüm alanları doldurun.");
                return;
            }

            // API'ye gidiyoruz
            var kullanici = await _apiService.LoginAsync(email, sifre);

            // KRİTİK KONTROL: Eğer API'den null döndüyse (Kullanıcı bulunamadıysa)
            if (kullanici == null)
            {
                ShowError("Giriş yapılamadı. Bilgilerinizi kontrol edin veya hesabınızın aktif olduğundan emin olun.");
                return;
            }

            // Veriler null gelirse çökmemesi için ?? operatörünü kullanıyoruz
            string userId = kullanici.Id.ToString();
            string tamAd = kullanici.TamAd ?? "Kullanıcı";
            string userEmail = kullanici.Email ?? email;

            Preferences.Default.Set("UserId", userId);
            Preferences.Default.Set("UserFullName", tamAd);
            Preferences.Default.Set("UserEmail", userEmail);
            Preferences.Default.Set("UserGender", kullanici.Cinsiyet);

            // Handle Remember Me preference: save or remove credentials
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

            // Shell yönlendirmesi
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
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
                // If unchecked, remove stored credentials immediately
                Preferences.Default.Set("RememberMe", false);
                Preferences.Default.Remove("SavedEmail");
                Preferences.Default.Remove("SavedPassword");
            }
            else
            {
                // If checked, we don't save until successful login to avoid storing invalid data
                Preferences.Default.Set("RememberMe", true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error handling RememberMe change: " + ex.Message);
        }
    }
}