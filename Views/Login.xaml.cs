using KampusRotaUI.Services;
using System.Diagnostics;

namespace KampusRotaUI.Views;

public partial class Login : ContentPage
{
    private readonly IApiService _apiService = new ApiServices();

    public Login()
    {
        InitializeComponent();
        Debug.WriteLine("LOGIN SAYFASI AÇILDI");
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
            string tamAd = kullanici.TamAd ?? "Kullanıcı";
            string userEmail = kullanici.Email ?? email;

            Preferences.Default.Remove("UserId");
            Preferences.Default.Set("UserId", kullanici.Id);
            Preferences.Default.Set("UserFullName", tamAd);
            Preferences.Default.Set("UserEmail", userEmail);
            Preferences.Default.Set("UserGender", kullanici.Cinsiyet);
            Preferences.Default.Set("RememberMe", RememberMeCheck?.IsChecked ?? false);

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
}
