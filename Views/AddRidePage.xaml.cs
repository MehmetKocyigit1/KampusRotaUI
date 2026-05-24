using System.Diagnostics;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class AddRidePage : ContentPage
{
    private const string MapAssetFileName = "map.html";
    private readonly ApiServices _apiService = new ApiServices();
    private MapInterop _mapInterop;
    private string _lastFocusedPickerName = "Departure";
    private string? _mapHtmlContent;

    public AddRidePage()
    {
        InitializeComponent();
        _mapInterop = new MapInterop();
        _mapInterop.LocationSelected += OnMapLocationSelected;

        LoadMapIntoWebView();

        // Intercept navigation from map (app://locationSelected fallback)
        MapWebView.Navigating += OnMapWebViewNavigating;
        MapWebView.Navigated += OnMapWebViewNavigated;
    }

    private void OnDepartureLocationTapped(object? sender, TappedEventArgs e)
    {
        _lastFocusedPickerName = "Departure";
        ShowMapView();
    }

    private void OnDestinationLocationTapped(object? sender, TappedEventArgs e)
    {
        _lastFocusedPickerName = "Destination";
        ShowMapView();
    }

    private void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!MapInterop.TryCreateSelectionFromUrl(e.Url, out var selection))
        {
            return;
        }

        e.Cancel = true;
        SelectLocationForCurrentPicker(selection.LocationName);
    }

    private void OnMapWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Debug.WriteLine($"MapWebView navigated to: {e?.Url}");
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
            Debug.WriteLine($"Error loading map file: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapDebugLabel.IsVisible = true;
                MapDebugLabel.Text = $"Harita yüklenemedi: {ex.Message}";
            });

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

    private void OnMapLocationSelected(object? sender, MapLocationSelectedEventArgs e)
    {
        SelectLocationForCurrentPicker(e.LocationName);
    }

    private void SelectLocationForCurrentPicker(string locationName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Picker targetPicker = _lastFocusedPickerName == "Destination" ? DestinationPicker : DeparturePicker;

            if (!targetPicker.Items.Contains(locationName))
            {
                targetPicker.Items.Add(locationName);
            }

            targetPicker.SelectedItem = locationName;
            UpdateLocationLabels();
            ShowFormView();
        });
    }

    private void ShowFormView()
    {
        FormView.IsVisible = true;
        MapContainer.IsVisible = false;
        MapDebugLabel.IsVisible = false;
    }

    private void ShowMapView()
    {
        FormView.IsVisible = false;
        MapContainer.IsVisible = true;
        MapDebugLabel.IsVisible = false;
        if (MapWebView.Source == null && !string.IsNullOrEmpty(_mapHtmlContent))
        {
            MapWebView.Source = new HtmlWebViewSource { Html = _mapHtmlContent };
        }
    }

    private void OnCloseMapClicked(object sender, EventArgs e)
    {
        ShowFormView();
    }

    private void UpdateLocationLabels()
    {
        UpdateLocationLabel(DepartureLocationLabel, DeparturePicker, "Kalkış noktası seçin");
        UpdateLocationLabel(DestinationLocationLabel, DestinationPicker, "Varış noktası seçin");
    }

    private static void UpdateLocationLabel(Label label, Picker picker, string placeholder)
    {
        var value = picker.SelectedItem?.ToString();
        label.Text = string.IsNullOrWhiteSpace(value) ? placeholder : value;
        label.TextColor = string.IsNullOrWhiteSpace(value)
            ? Color.FromArgb("#6B7280")
            : Color.FromArgb("#111827");
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        if (!TermsCheckBox.IsChecked)
        {
            await DisplayAlert("Hata", "Lütfen kampüs kurallarını kabul edin.", "Tamam");
            return;
        }

        var kalkis = DeparturePicker.SelectedItem?.ToString();
        var varis = DestinationPicker.SelectedItem?.ToString();
        var aciklama = DescriptionEditor.Text?.Trim() ?? string.Empty;

        int.TryParse(SeatsEntry.Text, out var koltukSayisi);
        decimal.TryParse(PriceEntry.Text, out var ucret);

        if (string.IsNullOrEmpty(kalkis) || string.IsNullOrEmpty(varis) || koltukSayisi <= 0)
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen rota ve en az 1 koltuk sayısı belirleyin.", "Tamam");
            return;
        }

        if (string.Equals(kalkis, varis, StringComparison.CurrentCultureIgnoreCase))
        {
            await DisplayAlert("Rota Hatası", "Kalkış ve varış noktası aynı olamaz.", "Tamam");
            return;
        }

        if (RideDatePicker.Date.Add(RideTimePicker.Time) <= DateTime.Now)
        {
            await DisplayAlert("Tarih Hatası", "Geçmiş tarih veya saat için ilan oluşturamazsın.", "Tamam");
            return;
        }

        if (koltukSayisi > 6)
        {
            await DisplayAlert("Koltuk Sayısı", "En fazla 6 boş koltuk belirleyebilirsin.", "Tamam");
            return;
        }

        if (ucret < 0)
        {
            await DisplayAlert("Ücret Hatası", "Ücret negatif olamaz.", "Tamam");
            return;
        }

        if (aciklama.Length > 250)
        {
            await DisplayAlert("Açıklama", "Açıklama en fazla 250 karakter olabilir.", "Tamam");
            return;
        }

        if (!TryGetCurrentUserId(out var surucuId))
        {
            await DisplayAlert("Oturum Hatası", "Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Tamam");
            return;
        }

        var yeniYolculuk = new Yolculuk
        {
            KalkisNoktasi = kalkis,
            VarisNoktasi = varis,
            KalkisZamani = RideDatePicker.Date.Add(RideTimePicker.Time),
            BosKoltukSayisi = koltukSayisi,
            KisiBasiUcret = ucret,
            Aciklama = aciklama,
            SadeceKadinlarMi = WomenOnlyCheckBox?.IsChecked ?? false,
            SurucuId = surucuId,
            AktifMi = true,
            SilindiMi = false
        };

        try
        {
            bool success = await _apiService.YolculukEkleAsync(yeniYolculuk, yeniYolculuk.SurucuId);

            if (success)
            {
                await DisplayAlert("Başarılı", "Yolculuk ilanınız başarıyla yayınlandı! 🚗", "Harika");
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

    private static bool TryGetCurrentUserId(out int userId)
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        return int.TryParse(userIdText, out userId) && userId > 0;
    }
}
