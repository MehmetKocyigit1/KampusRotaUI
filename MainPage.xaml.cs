using System.Collections.ObjectModel;
using System.Diagnostics;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class MainPage : ContentPage
{
    private readonly IApiService _apiService;

    // Modelimizi İngilizce 'Ride' yerine yeni Türkçe 'Yolculuk' modelimize çevirdik
    public ObservableCollection<Yolculuk> Rides { get; set; } = new();

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiServices();
        RidesCollection.ItemsSource = Rides;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            // Servisimizdeki yeni metodu çağırıyoruz
            var rides = await _apiService.TumYolculuklariGetirAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Rides.Clear();
                foreach (var ride in rides)
                {
                    Rides.Add(ride);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Veri çekme hatası: {ex.Message}");
        }
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        string selectedStart = StartPicker.SelectedItem?.ToString();
        DateTime selectedDate = RideDate.Date;

        if (string.IsNullOrEmpty(selectedStart))
        {
            await DisplayAlert("Uyarı", "Lütfen bir kalkış noktası seçin.", "Tamam");
            return;
        }

        try
        {
            var filteredRides = await _apiService.YolculuklariAraAsync(selectedStart, null, selectedDate, null);

            Rides.Clear();
            foreach (var ride in filteredRides)
            {
                Rides.Add(ride);
            }

            if (filteredRides.Count == 0)
                await DisplayAlert("Bilgi", "Seçilen kriterlere uygun ilan bulunamadı.", "Tamam");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Arama hatası: {ex.Message}");
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        // Çıkış yaparken cihaz hafızasındaki (Preferences) oturum bilgilerini temizliyoruz
        Preferences.Default.Clear();
        Application.Current.MainPage = new NavigationPage(new Login());
    }

    // Ana sayfa üzerinden "Hızlı İlan Ver" mantığı
    private async void OnPublishClicked(object sender, EventArgs e)
    {
        try
        {
            int userId = Preferences.Default.Get("UserId", 0);

            if (userId == 0)
            {
                await DisplayAlert("Hata", "Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Tamam");
                return;
            }

            // Yeni Türkçe modele göre hızlı ilan nesnesi
            var yeniYolculuk = new Yolculuk
            {
                SurucuId = userId,
                KalkisNoktasi = "Doğu Kampüsü", // İstersen XAML'de kalkış picker'ı ekleyip dinamik yapabilirsin
                KalkisZamani = DateTime.Now,

                // Arka plan takip verileri
                AktifMi = true,
                SilindiMi = false
            };

            // Yeni servis metodumuz ile API'ye gönderiyoruz
            bool success = await _apiService.YolculukEkleAsync(yeniYolculuk, userId);

           
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Bir sorun oluştu: " + ex.Message, "Tamam");
        }
    }

    private async void OnEditRideClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Yolculuk yolculuk)
        {
            return;
        }

        int userId = Preferences.Default.Get("UserId", 0);
        if (userId == 0)
        {
            await DisplayAlert("Oturum Hatası", "Lütfen tekrar giriş yapın.", "Tamam");
            return;
        }

        if (yolculuk.SurucuId != userId)
        {
            await DisplayAlert("Yetki Yok", "Sadece kendi ilanınızı düzenleyebilirsiniz.", "Tamam");
            return;
        }

        await Navigation.PushAsync(new EditRidePage(yolculuk));
    }

    private async void OnDeleteRideClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Yolculuk yolculuk)
        {
            return;
        }

        int userId = Preferences.Default.Get("UserId", 0);
        if (userId == 0)
        {
            await DisplayAlert("Oturum Hatası", "Lütfen tekrar giriş yapın.", "Tamam");
            return;
        }

        if (yolculuk.SurucuId != userId)
        {
            await DisplayAlert("Yetki Yok", "Sadece kendi ilanınızı silebilirsiniz.", "Tamam");
            return;
        }

        bool confirm = await DisplayAlert("İlanı Sil", "Bu yolculuk ilanını silmek istiyor musunuz?", "Sil", "Vazgeç");
        if (!confirm)
        {
            return;
        }

        bool success = await _apiService.YolculukSilAsync(yolculuk.Id, userId);
        if (!success)
        {
            await DisplayAlert("Hata", "İlan silinemedi.", "Tamam");
            return;
        }

        Rides.Remove(yolculuk);
    }
}
