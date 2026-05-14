using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class RegisterPage : ContentPage
{
    private readonly ApiServices _apiService = new ApiServices();

    public RegisterPage()
    {
        InitializeComponent();
    }

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
        string cinsiyet = GetSelectedGender();


        try
        {
            // 1. Boş Alan Kontrolü
            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre) ||
                string.IsNullOrWhiteSpace(cinsiyet))
            {
                await DisplayAlert("Eksik Bilgi", "Lütfen tüm zorunlu alanları doldurun.", "Tamam");
                return;
            }

             if (!email.EndsWith(".edu.tr"))
            {
                await DisplayAlert("Geçersiz E-posta", "Sisteme sadece üniversite uzantılı (.edu.tr) e-posta ile kayıt olunabilir.", "Tamam");
                return;
            }

            // 3. Şifre Kontrolleri
            if (sifre.Length < 6)
            {
                await DisplayAlert("Güvenlik", "Şifreniz en az 6 karakter olmalıdır.", "Tamam");
                return;
            }

            if (sifre != sifreTekrar)
            {
                await DisplayAlert("Hata", "Girdiğiniz şifreler birbiriyle eşleşmiyor.", "Tamam");
                return;
            }

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

            // 5. API'ye Gönder
            var kayitliKullanici = await _apiService.KayitOlAsync(yeniKullanici);

            if (kayitliKullanici != null)
            {
                await DisplayAlert("Başarılı!", "Hesabınız başarıyla oluşturuldu. Şimdi giriş yapabilirsiniz.", "Harika");

                // Kayıt başarılıysa Login sayfasına geri dön
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Hata", "Kayıt işlemi başarısız oldu. Bu e-posta zaten kullanımda olabilir.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Register] Hata: {ex.Message}");
            await DisplayAlert("Bağlantı Hatası", "Sunucuya ulaşılamıyor.", "Tamam");
        }
        finally
        {
            registerButton.IsEnabled = true;
        }
    }

    private async void OnLoginNavClicked(object sender, EventArgs e)
    {
         await Navigation.PopAsync();
    }

    private string GetSelectedGender()
    {
        if (FemaleGenderRadio.IsChecked)
            return "Kadın";

        if (MaleGenderRadio.IsChecked)
            return "Erkek";

        if (UnspecifiedGenderRadio.IsChecked)
            return "Belirtmek İstemiyorum";

        return string.Empty;
    }
}
