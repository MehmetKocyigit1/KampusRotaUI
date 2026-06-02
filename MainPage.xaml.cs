using System.Collections.ObjectModel;
using System.Diagnostics;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class MainPage : ContentPage
{
    private const string MapAssetFileName = "map.html";
    private const double MaxAutoSelectDistanceMeters = 30000;
    private readonly ApiServices _apiService;
    private MapInterop _mapInterop;
    private Picker? _mapTargetPicker;
    private string? _mapHtmlContent;
    private bool _returnToSearchAfterMap;
    private static readonly IReadOnlyList<MapPlace> MapPlaces = new[]
    {
        new MapPlace("Iyaş", 37.7820357443819, 30.544646520371813),
        new MapPlace("Doğu Kampüsü", 37.8285, 30.5345),
        new MapPlace("Batı Kampüsü", 37.829387079488605, 30.52643484819006),
        new MapPlace("100. Yıl Yerleşkesi", 37.759683693975894, 30.548229307533997),
        new MapPlace("Erkek Yurdu", 37.782313447651674, 30.56188560473612),
        new MapPlace("MihriHatun Kız Yurdu", 37.85498503730966, 30.53145637770997),
        new MapPlace("Şehir Merkezi (Meydan)", 37.7630, 30.5547),
        new MapPlace("Otogar", 37.810292530456906, 30.537213639241752)
    };

    // Modelimizi İngilizce 'Ride' yerine yeni Türkçe 'Yolculuk' modelimize çevirdik
    public ObservableCollection<Yolculuk> Rides { get; set; } = new();

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiServices();
        RidesCollection.ItemsSource = Rides;
        
        _mapInterop = new MapInterop();
        _mapInterop.LocationSelected += OnMapLocationSelected;
        MapWebView.Navigating += OnMapWebViewNavigating;
        _mapTargetPicker = StartPicker;
        
        LoadMapIntoWebView();
    }

    private async void LoadMapIntoWebView()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(MapAssetFileName);
            using var reader = new StreamReader(stream);
            _mapHtmlContent = await reader.ReadToEndAsync();

            MapWebView.Source = await CreateMapSourceAsync(_mapHtmlContent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading map: {ex.Message}");

            if (!string.IsNullOrWhiteSpace(_mapHtmlContent))
            {
                MapWebView.Source = new HtmlWebViewSource { Html = _mapHtmlContent };
            }
        }
    }

    private static async Task<WebViewSource> CreateMapSourceAsync(string html)
    {
        await Task.CompletedTask;
        return new HtmlWebViewSource { Html = html };
    }

    private void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!MapInterop.TryCreateSelectionFromUrl(e.Url, out var selection))
        {
            return;
        }

        e.Cancel = true;
        SelectMapLocation(selection.LocationName);
    }

    private void OnMapLocationSelected(object? sender, MapLocationSelectedEventArgs e)
    {
        SelectMapLocation(e.LocationName);
    }

    private void SelectMapLocation(string locationName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var targetPicker = _mapTargetPicker ?? StartPicker;

            if (!targetPicker.Items.Contains(locationName))
            {
                targetPicker.Items.Add(locationName);
            }

            targetPicker.SelectedItem = locationName;
            UpdateLocationLabels();
            if (_returnToSearchAfterMap)
            {
                ShowSearchView();
            }
            else
            {
                ShowListView();
            }
        });
    }

    private void OnStartLocationTapped(object? sender, TappedEventArgs e)
    {
        OpenMapForPicker(StartPicker);
    }

    private void OnDestinationLocationTapped(object? sender, TappedEventArgs e)
    {
        OpenMapForPicker(DestinationPicker);
    }

    private void OnDestinationMapClicked(object sender, EventArgs e)
    {
        OpenMapForPicker(DestinationPicker);
    }

    private void OpenMapForPicker(Picker targetPicker)
    {
        _mapTargetPicker = targetPicker;
        _returnToSearchAfterMap = SearchViewContainer.IsVisible;
        ShowMapView();
    }

    private void ShowListView()
    {
        ListViewContainer.IsVisible = true;
        SearchViewContainer.IsVisible = false;
        MapContainer.IsVisible = false;
    }

    private void ShowSearchView()
    {
        ListViewContainer.IsVisible = false;
        SearchViewContainer.IsVisible = true;
        MapContainer.IsVisible = false;
    }

    private void ShowMapView()
    {
        ListViewContainer.IsVisible = false;
        SearchViewContainer.IsVisible = false;
        MapContainer.IsVisible = true;
        if (MapWebView.Source == null && !string.IsNullOrEmpty(_mapHtmlContent))
        {
            MapWebView.Source = new HtmlWebViewSource { Html = _mapHtmlContent };
        }
    }

    private void OnCloseMapClicked(object sender, EventArgs e)
    {
        if (_returnToSearchAfterMap)
        {
            ShowSearchView();
            return;
        }

        ShowListView();
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
            await DeleteExpiredRidesAsync(rides);
            var activeRides = rides
                .Where(r => r.AktifMi && !r.SilindiMi)
                .OrderByDescending(r => r.KalkisZamani >= DateTime.Now)
                .ThenBy(r => r.KalkisZamani)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Rides.Clear();
                foreach (var ride in activeRides)
                {
                    Rides.Add(ride);
                }

                ResultsTitleLabel.Text = "Mevcut Yolculuklar";
                ResultsSubtitleLabel.Text = activeRides.Count == 0
                    ? "Şu anda aktif ilan yok"
                    : $"{activeRides.Count} aktif ilan listeleniyor";
                ActiveFilterLabel.Text = "Kalkış ve varış seçerek kampüs rotalarını filtrele.";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Veri çekme hatası: {ex.Message}");
        }
    }

    private void OnOpenSearchClicked(object sender, EventArgs e)
    {
        UpdateLocationLabels();
        ShowSearchView();
    }

    private void OnCloseSearchClicked(object sender, EventArgs e)
    {
        ShowListView();
    }

    private async void OnResetSearchClicked(object sender, EventArgs e)
    {
        StartPicker.SelectedItem = null;
        DestinationPicker.SelectedItem = null;
        RideDate.Date = DateTime.Today;
        UpdateLocationLabels();
        await LoadData();
        ShowListView();
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        var selectedStart = StartPicker.SelectedItem?.ToString();
        var selectedDestination = DestinationPicker.SelectedItem?.ToString();
        var selectedDate
            = RideDate.Date;

        if (string.IsNullOrEmpty(selectedStart) || string.IsNullOrEmpty(selectedDestination))
        {
            await DisplayAlert("Uyarı", "Lütfen kalkış ve varış noktası seçin.", "Tamam");
            return;
        }

        try
        {
            var allRides = await _apiService.TumYolculuklariGetirAsync();

            // Filtrelemeyi yeni Türkçe özelliklere (KalkisNoktasi, KalkisZamani) göre yapıyoruz
            var filteredRides = allRides.Where(r =>
                r.AktifMi &&
                !r.SilindiMi &&
                r.KalkisZamani >= DateTime.Now &&
                r.KalkisZamani.Date == selectedDate.Date &&
                string.Equals(r.KalkisNoktasi, selectedStart, StringComparison.CurrentCultureIgnoreCase) &&
                string.Equals(r.VarisNoktasi, selectedDestination, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(r => r.KalkisZamani)
                .ToList();

            Rides.Clear();
            foreach (var ride in filteredRides)
            {
                Rides.Add(ride);
            }

            ResultsTitleLabel.Text = "Arama Sonuçları";
            ResultsSubtitleLabel.Text = filteredRides.Count == 0
                ? "Bu kriterlerle uygun ilan bulunamadı"
                : $"{filteredRides.Count} uygun ilan bulundu";
            ActiveFilterLabel.Text = $"{selectedStart} → {selectedDestination} · {selectedDate:dd MMM yyyy}";
            ShowListView();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Arama hatası: {ex.Message}");
        }
    }

    private async void OnUseCurrentLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Konum", "Mevcut konumu kullanmak için konum izni gerekiyor.", "Tamam");
                return;
            }

            var location = await GetCurrentLocationAsync();
            if (location == null)
            {
                await DisplayAlert("Konum", "Mevcut konum alınamadı.", "Tamam");
                return;
            }

            var nearestPlace = MapPlaces
                .Select(place => new
                {
                    Place = place,
                    DistanceMeters = DistanceInMeters(location.Latitude, location.Longitude, place.Latitude, place.Longitude)
                })
                .OrderBy(result => result.DistanceMeters)
                .First();

            if (nearestPlace.DistanceMeters > MaxAutoSelectDistanceMeters)
            {
                await DisplayAlert(
                    "Konum",
                    "Cihazın döndürdüğü konum Isparta duraklarına uzak görünüyor. Lütfen Android emülatörde/cihazda konumun Isparta olarak ayarlı olduğundan emin ol veya haritadan pin seç.",
                    "Tamam");
                return;
            }

            StartPicker.SelectedItem = nearestPlace.Place.Name;
            UpdateLocationLabels();
            await DisplayAlert(
                "Konum Seçildi",
                $"Mevcut konumuna en yakın durak: {nearestPlace.Place.Name} ({FormatDistance(nearestPlace.DistanceMeters)})",
                "Tamam");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Konum alma hatası: {ex.Message}");
            await DisplayAlert("Konum", "Konum alınırken bir sorun oluştu.", "Tamam");
        }
    }

    private async void OnRideDetailsClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Yolculuk ride })
        {
            return;
        }

        await Navigation.PushAsync(new RideDetailPage(ride));
    }

    private async Task DeleteExpiredRidesAsync(IEnumerable<Yolculuk> rides)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0)
        {
            return;
        }

        var expiredRides = rides
            .Where(r => !r.SilindiMi && r.KalkisZamani < DateTime.Now)
            .ToList();

        foreach (var ride in expiredRides)
        {
            await _apiService.YolculukSilAsync(ride.Id, currentUserId);
        }
    }

    private static int GetCurrentUserId()
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        if (int.TryParse(userIdText, out var userId))
        {
            return userId;
        }

        return Preferences.Default.Get("UserId", 0);
    }

    private static async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (IsValidLocation(location))
            {
                return location;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Anlık konum alınamadı: {ex.Message}");
        }

        try
        {
            var lastKnownLocation = await Geolocation.Default.GetLastKnownLocationAsync();
            return IsValidLocation(lastKnownLocation) ? lastKnownLocation : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Son bilinen konum alınamadı: {ex.Message}");
            return null;
        }
    }

    private static bool IsValidLocation(Location? location)
    {
        return location is not null &&
               !double.IsNaN(location.Latitude) &&
               !double.IsNaN(location.Longitude) &&
               Math.Abs(location.Latitude) <= 90 &&
               Math.Abs(location.Longitude) <= 180;
    }

    private static double DistanceInMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6371000;
        var latDistance = DegreesToRadians(lat2 - lat1);
        var lngDistance = DegreesToRadians(lng2 - lng1);
        var lat1Radians = DegreesToRadians(lat1);
        var lat2Radians = DegreesToRadians(lat2);

        var a = Math.Sin(latDistance / 2) * Math.Sin(latDistance / 2) +
                Math.Cos(lat1Radians) * Math.Cos(lat2Radians) *
                Math.Sin(lngDistance / 2) * Math.Sin(lngDistance / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private static string FormatDistance(double distanceMeters)
    {
        return distanceMeters >= 1000
            ? $"{distanceMeters / 1000:0.0} km"
            : $"{distanceMeters:0} m";
    }

    private void UpdateLocationLabels()
    {
        UpdateLocationLabel(StartLocationLabel, StartPicker, "Kalkış noktası");
        UpdateLocationLabel(DestinationLocationLabel, DestinationPicker, "Varış noktası");
    }

    private static void UpdateLocationLabel(Label label, Picker picker, string placeholder)
    {
        var value = picker.SelectedItem?.ToString();
        label.Text = string.IsNullOrWhiteSpace(value) ? placeholder : value;
        label.TextColor = string.IsNullOrWhiteSpace(value)
            ? Color.FromArgb("#6B7280")
            : Color.FromArgb("#111827");
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        // Çıkış yaparken cihaz hafızasındaki (Preferences) oturum bilgilerini temizliyoruz
        Preferences.Default.Clear();
        if (Application.Current is not null)
        {
            Application.Current.MainPage = new NavigationPage(new Login());
        }
    }

    // Ana sayfa üzerinden "Hızlı İlan Ver" mantığı
    private async void OnPublishClicked(object sender, EventArgs e)
    {
        try
        {
            var userIdText = Preferences.Default.Get("UserId", "0");
            if (!int.TryParse(userIdText, out var userId) || userId == 0)
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

    private sealed record MapPlace(string Name, double Latitude, double Longitude);
}
