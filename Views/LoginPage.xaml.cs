using KampusRotaUI.Services;
using KampusRotaUI.Models;
using System.Diagnostics;

namespace KampusRotaUI.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        Debug.WriteLine("LOGIN PAGE AÇILDI");
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("LOGIN CLICKED");
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Lütfen tüm alanları doldurun.");
            return;
        }

        try
        {
            var apiService = new ApiServices();

            var user = await apiService.LoginAsync(email, password);

            if (user != null)
            {
                Preferences.Default.Set("UserId", user.Id);
                Preferences.Default.Set("UserName", user.Ad);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // AppShell üzerinden uygulamanın ana yapısına geçiyoruz
                    Application.Current.MainPage = new AppShell();
                });
            }
            else
            {
                ShowError("E-posta veya şifre hatalı!");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Login] Hata: {ex.Message}");
            ShowError("Bağlantı hatası: Sunucuya ulaşılamıyor.");
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnRegisterNavClicked(object sender, EventArgs e)
    {
        // Kayıt sayfasına yönlendirme (Eğer RegisterPage varsa aktif edebilirsin)
        // await Navigation.PushAsync(new RegisterPage());
        await DisplayAlert("Bilgi", "Kayıt sayfası şu an yapım aşamasında.", "Tamam");
    }
}