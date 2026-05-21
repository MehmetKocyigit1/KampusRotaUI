using System.Diagnostics;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class AddRidePage : ContentPage
{
    // Servis bağlantımız
    private readonly ApiServices _apiService = new ApiServices();

    public AddRidePage()
    {
        InitializeComponent();
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        // 1. Kuralların kabul edilip edilmediğini kontrol et
        if (TermsCheckBox == null || !TermsCheckBox.IsChecked)
        {
            await DisplayAlert("Hata", "Lütfen kampüs kurallarını kabul edin.", "Tamam");
            return;
        }

        // 2. Picker ve Entry verilerini güvenli bir şekilde al
        string kalkis = DeparturePicker.SelectedItem?.ToString();
        string varis = DestinationPicker.SelectedItem?.ToString();
        string aciklama = DescriptionEditor?.Text ?? string.Empty; // Kullanıcı açıklama girmezse boş string ata

        // Koltuk sayısı ve ücreti sayıya çevirirken uygulama çökmesin diye TryParse kullanıyoruz
        int.TryParse(SeatsEntry?.Text, out int koltukSayisi);
        decimal.TryParse(PriceEntry?.Text, out decimal ucret);

        // 3. Basit bir boş alan kontrolü
        if (string.IsNullOrEmpty(kalkis) || string.IsNullOrEmpty(varis) || koltukSayisi <= 0)
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen rota ve en az 1 koltuk sayısı belirleyin.", "Tamam");
            return;
        }

        // 4. Model nesnesini oluştur (Yeni Türkçe 'Yolculuk' modelimize %100 uyumlu)
        // 1. Önce veriyi dışarıda hazırla (Süslü parantezin DIŞINDA)
        string userIdStr = Preferences.Default.Get("UserId", "0");
        int surucuId = int.Parse(userIdStr);

        // 2. Şimdi nesneyi oluştur
        var yeniYolculuk = new Yolculuk
        {
            KalkisNoktasi = kalkis,
            VarisNoktasi = varis,
            KalkisZamani = RideDatePicker.Date.Add(RideTimePicker.Time),
            BosKoltukSayisi = koltukSayisi,
            KisiBasiUcret = ucret,
            Aciklama = aciklama,
            SadeceKadinlarMi = WomenOnlyCheckBox?.IsChecked ?? false,

            // Değişkeni burada sadece atıyoruz:
            SurucuId = surucuId,

            AktifMi = true,
            SilindiMi = false
        };

        try
        {
            // 5. API'ye gönder (ApiServices içinde bu metodu güncellediğimizi varsayıyoruz)
            bool success = await _apiService.YolculukEkleAsync(yeniYolculuk, yeniYolculuk.SurucuId);

            if (success)
            {
                await DisplayAlert("Başarılı", "Yolculuk ilanınız başarıyla yayınlandı! 🚗", "Harika");

                // Bir önceki sayfaya (Ana Sayfaya) geri dön
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Hata", "İlan sunucuya gönderilemedi. Lütfen bağlantınızı kontrol edin.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddRide] Hata: {ex.Message}");
            await DisplayAlert("Sistem Hatası", "Beklenmedik bir hata oluştu. Daha sonra tekrar deneyin.", "Tamam");
        }
    }
}