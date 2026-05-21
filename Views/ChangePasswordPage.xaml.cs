using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class ChangePasswordPage : ContentPage
{
    // API ile iletişim kuracak servisimiz
    private readonly IApiService _apiService = new ApiServices();

    public ChangePasswordPage()
    {
        InitializeComponent();
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        MessageLabel.IsVisible = false;

        string oldPass = OldPasswordEntry.Text;
        string newPass = NewPasswordEntry.Text;
        string confirmPass = ConfirmPasswordEntry.Text;

        // 1. Boş alan kontrolü
        if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
        {
            ShowMessage("Tüm alanları doldurmalısınız!", Colors.Red);
            return;
        }

        // 2. Şifre uzunluk kontrolü
        if (newPass.Length < 6)
        {
            ShowMessage("Yeni şifre en az 6 karakter olmalıdır.", Colors.Red);
            return;
        }

        // 3. Şifre eşleşme kontrolü
        if (newPass != confirmPass)
        {
            ShowMessage("Yeni şifreler birbiriyle eşleşmiyor!", Colors.Red);
            return;
        }

        // 4. Giriş yapan kullanıcının ID'sini Preferences'tan (Cihaz Hafızasından) alıyoruz
        int userId = Preferences.Default.Get("UserId", 0);

        if (userId == 0)
        {
            ShowMessage("Oturum hatası. Lütfen uygulamaya tekrar giriş yapın.", Colors.Red);
            return;
        }

        // 5. Yeni oluşturduğumuz Türkçe modele göre isteği paketliyoruz
        var istek = new SifreDegistirmeIstegi
        {
            EskiSifre = oldPass,
            YeniSifre = newPass,
            YeniSifreTekrar = confirmPass
        };

        // Çoklu tıklamayı engellemek için butonu geçici olarak pasif yap
        var button = (Button)sender;
        button.IsEnabled = false;

        try
        {
            // 6. Gerçek API İsteği (ApiServices dosyasında bu metodun var olduğunu varsayıyoruz)
            bool success = await _apiService.SifreDegistirAsync(userId, istek);

            if (success)
            {
                ShowMessage("Şifreniz başarıyla güncellendi!", Colors.Green);

                // Kutuları temizle
                OldPasswordEntry.Text = string.Empty;
                NewPasswordEntry.Text = string.Empty;
                ConfirmPasswordEntry.Text = string.Empty;

                await DisplayAlert("Başarılı", "Şifreniz güvenli bir şekilde değiştirildi.", "Tamam");

                // İşlem başarıyla bitince bir önceki sayfaya (Ayarlar/Profil) geri dön
                await Navigation.PopAsync();
            }
            else
            {
                ShowMessage("Mevcut şifreniz hatalı veya işlem reddedildi.", Colors.Red);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChangePassword] Hata: {ex.Message}");
            ShowMessage("Sunucuya ulaşılamadı. Lütfen internet bağlantınızı kontrol edin.", Colors.Red);
        }
        finally
        {
            // İşlem bittikten (başarılı veya hatalı) sonra butonu tekrar aktif et
            button.IsEnabled = true;
        }
    }

    private void ShowMessage(string message, Color color)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = color;
        MessageLabel.IsVisible = true;
    }
}
