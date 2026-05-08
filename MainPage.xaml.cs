using System.Collections.ObjectModel;
using System.Diagnostics;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class MainPage : ContentPage
{
    private readonly ApiServices _apiService;

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
            var allRides = await _apiService.TumYolculuklariGetirAsync();

            // Filtrelemeyi yeni Türkçe özelliklere (KalkisNoktasi, KalkisZamani) göre yapıyoruz
            var filteredRides = allRides.Where(r =>
                r.KalkisNoktasi == selectedStart &&
                r.KalkisZamani.Date == selectedDate.Date).ToList();

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
}