using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KampusRotaUI.Models;
using KampusRotaUI.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace KampusRotaUI.Views;

public partial class AddRidePage : ContentPage
{
    private const string MapAssetFileName = "map.html";
    private readonly ApiServices _apiService = new ApiServices();
    private readonly Yolculuk? _editingRide;
    private MapInterop? _mapInterop;
    private string _lastFocusedPickerName = "Departure";
    private string? _mapHtmlContent;
    private string _registeredPhoneNumber = string.Empty;
    private bool _isUpdatingRegisteredPhoneOption;
    private bool _isUpdatingWomenOnlyAvailability;

    private List<University> _universities = new();
    private University? _currentMapUniversity;
    private double? _departureLatitude;
    private double? _departureLongitude;
    private double? _destinationLatitude;
    private double? _destinationLongitude;

    public AddRidePage()
    {
        InitializeComponent();
        InitializePage();
        ConfigureRegisteredPhoneOption();
        ConfigureWomenOnlyOption();
    }

    public AddRidePage(Yolculuk ride)
    {
        InitializeComponent();
        _editingRide = ride;
        InitializePage();
        ConfigureEditMode(ride);
        ConfigureRegisteredPhoneOption();
        ConfigureWomenOnlyOption();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SetupMapForSelectedUniversityAsync();
    }

    private void InitializePage()
    {
        _mapInterop = new MapInterop();
        _mapInterop.LocationSelected += OnMapLocationSelected;

#if WINDOWS
        MapWebView.HandlerChanged += (s, e) =>
        {
            if (MapWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
            {
                webView2.WebMessageReceived += (sender, args) =>
                {
                    try
                    {
                        var raw = args.TryGetWebMessageAsString();
                        if (string.IsNullOrEmpty(raw))
                        {
                            raw = args.WebMessageAsJson;
                        }
                        if (!string.IsNullOrEmpty(raw))
                        {
                            _mapInterop.SelectLocation(raw);
                        }
                    }
                    catch { }
                };
            }
        };
#endif

        LoadMapIntoWebView();

        // Intercept navigation from map (app://locationSelected fallback)
        MapWebView.Navigating += OnMapWebViewNavigating;
        MapWebView.Navigated += OnMapWebViewNavigated;
    }

    private void ConfigureEditMode(Yolculuk ride)
    {
        Title = "İlanı Düzenle";
        PageTitleLabel.Text = "Yolculuk ilanını düzenle";
        PageSubtitleLabel.Text = "Yayınladığın ilan bilgilerini güncelle.";
        SaveRideButton.Text = "İlanı güncelle";
        TermsCheckBox.IsChecked = true;

        _departureLatitude = ride.KalkisLatitude;
        _departureLongitude = ride.KalkisLongitude;
        _destinationLatitude = ride.VarisLatitude;
        _destinationLongitude = ride.VarisLongitude;

        SelectPickerValue(DeparturePicker, ride.KalkisNoktasi);
        SelectPickerValue(DestinationPicker, ride.VarisNoktasi);
        UpdateLocationLabels();

        RideDatePicker.Date = ride.KalkisZamani.Date;
        RideTimePicker.Time = ride.KalkisZamani.TimeOfDay;
        PriceEntry.Text = ride.KisiBasiUcret.ToString("0.##");
        SeatsEntry.Text = ride.BosKoltukSayisi.ToString();
        ContactPhoneEntry.Text = ride.IletisimTelefonu;
        DescriptionEditor.Text = ride.Aciklama;
        WomenOnlyCheckBox.IsChecked = ride.SadeceKadinlarMi;
    }

    private void ConfigureRegisteredPhoneOption()
    {
        _registeredPhoneNumber = Preferences.Default.Get("UserPhone", string.Empty).Trim();
        var hasRegisteredPhone = !string.IsNullOrWhiteSpace(_registeredPhoneNumber);

        RegisteredPhoneShareBorder.IsVisible = hasRegisteredPhone;
        if (!hasRegisteredPhone)
        {
            return;
        }

        RegisteredPhoneDescriptionLabel.Text = $"Profilindeki numara: {_registeredPhoneNumber}";

        var currentPhone = ContactPhoneEntry.Text?.Trim() ?? string.Empty;
        var usesRegisteredPhone = string.Equals(currentPhone, _registeredPhoneNumber, StringComparison.OrdinalIgnoreCase);

        _isUpdatingRegisteredPhoneOption = true;
        ShareRegisteredPhoneCheckBox.IsChecked = usesRegisteredPhone;
        ContactPhoneEntry.IsEnabled = !usesRegisteredPhone;
        _isUpdatingRegisteredPhoneOption = false;
    }

    private static void SelectPickerValue(Picker picker, string value)
    {
        if (!picker.Items.Contains(value))
        {
            picker.Items.Add(value);
        }

        picker.SelectedItem = value;
    }

    private void ConfigureWomenOnlyOption()
    {
        _isUpdatingWomenOnlyAvailability = true;

        var isFemaleUser = IsCurrentUserFemale();
        WomenOnlyCheckBox.IsEnabled = isFemaleUser;

        if (!isFemaleUser)
        {
            WomenOnlyCheckBox.IsChecked = false;
            WomenOnlyBorder.Opacity = 0.58;
            WomenOnlyTitleLabel.TextColor = Color.FromArgb("#6B7280");
            WomenOnlyDescriptionLabel.Text = "Bu seçenek yalnızca kadın üyeler tarafından kullanılabilir.";
        }
        else
        {
            WomenOnlyBorder.Opacity = 1;
            WomenOnlyTitleLabel.TextColor = Color.FromArgb("#111827");
            WomenOnlyDescriptionLabel.Text = "İlanı kadın yolcular için görünür yap.";
        }

        _isUpdatingWomenOnlyAvailability = false;
    }

    private async void OnDepartureLocationTapped(object? sender, TappedEventArgs e)
    {
        _lastFocusedPickerName = "Departure";
        MapHeaderLabel.Text = "📍 Kalkış Noktası Seçin (Pin'e Dokunun)";
        ShowMapView();
        await SetupMapForSelectedUniversityAsync();
    }

    private async void OnDestinationLocationTapped(object? sender, TappedEventArgs e)
    {
        _lastFocusedPickerName = "Destination";
        MapHeaderLabel.Text = "🎯 Varış Noktası Seçin (Pin'e Dokunun)";
        ShowMapView();
        await SetupMapForSelectedUniversityAsync();
    }

    private void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!MapInterop.TryCreateSelectionFromUrl(e.Url, out var selection))
        {
            return;
        }

        e.Cancel = true;
        SelectLocationForCurrentPicker(selection.LocationName, selection.Latitude, selection.Longitude);
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
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            var html = await reader.ReadToEndAsync();

            var userUniId = Preferences.Default.Get("UserUniversityId", 0);
            if (userUniId > 0)
            {
                html = html.Replace("let currentUniId = null;", $"let currentUniId = {userUniId};");
            }

            _mapHtmlContent = html;
            MapWebView.Source = await CreateMapSourceAsync(_mapHtmlContent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading map file: {ex.Message}");

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

    private async Task<bool> WaitForMapReadyAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var res = await MapWebView.EvaluateJavaScriptAsync("(function(){return window.mapReady===true;})()");
                if (!string.IsNullOrWhiteSpace(res) && res.Trim().ToLower().Contains("true"))
                {
                    return true;
                }
            }
            catch { }

            await Task.Delay(250);
        }
        return false;
    }

    private async Task SetupMapForSelectedUniversityAsync()
    {
        try
        {
            if (_universities.Count == 0)
            {
                var unis = await _apiService.GetUniversitiesAsync();
                if (unis != null && unis.Count > 0)
                {
                    _universities = unis;
                    FormUniversityPicker.ItemsSource = _universities;
                }
            }

            var userUniId = Preferences.Default.Get("UserUniversityId", 0);
            University? targetUni = null;
            if (userUniId > 0 && _universities.Count > 0)
            {
                targetUni = _universities.FirstOrDefault(u => u.Id == userUniId);
            }

            if (targetUni == null && _universities.Count > 0)
            {
                targetUni = _universities[0];
                Preferences.Default.Set("UserUniversityId", targetUni.Id);
                Preferences.Default.Set("UserUniversityName", targetUni.Name);
                Preferences.Default.Set("UserCity", targetUni.City);
            }

            if (targetUni != null)
            {
                _currentMapUniversity = targetUni;
                FormUniversityPicker.SelectedItem = targetUni;
                FormUniversityLabel.Text = targetUni.Name;
                await FocusUniversityOnMapAsync(targetUni);
                await LoadLocationsForPickersAsync(targetUni.Id);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Harita üniversite ayarlama hatası: {ex.Message}");
        }
    }

    private async void OnFormUniversityPickerChanged(object? sender, EventArgs e)
    {
        if (FormUniversityPicker.SelectedItem is University selectedUni)
        {
            _currentMapUniversity = selectedUni;
            FormUniversityLabel.Text = selectedUni.Name;
            Preferences.Default.Set("UserUniversityId", selectedUni.Id);
            Preferences.Default.Set("UserUniversityName", selectedUni.Name);
            Preferences.Default.Set("UserCity", selectedUni.City);

            await FocusUniversityOnMapAsync(selectedUni);
            await LoadLocationsForPickersAsync(selectedUni.Id);
        }
    }

    private async Task FocusUniversityOnMapAsync(University uni)
    {
        var ready = await WaitForMapReadyAsync(TimeSpan.FromSeconds(3));
        if (!ready) return;

        try
        {
            await MapWebView.EvaluateJavaScriptAsync($"if(window.selectUniversityById) {{ window.selectUniversityById({uni.Id}); }}");

            var latStr = uni.Latitude.ToString(CultureInfo.InvariantCulture);
            var lngStr = uni.Longitude.ToString(CultureInfo.InvariantCulture);
            await MapWebView.EvaluateJavaScriptAsync($"focusUniversity({latStr}, {lngStr}, {uni.DefaultZoom});");

            var locations = await _apiService.GetCampusLocationsAsync(uni.Id);
            if (locations != null && locations.Count > 0)
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(locations, options);
                var escapedJson = JsonSerializer.Serialize(json);
                await MapWebView.EvaluateJavaScriptAsync($"loadCampusPlaces({escapedJson});");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Harita odaklama hatası: {ex.Message}");
        }
    }

    private async Task LoadLocationsForPickersAsync(int universityId)
    {
        try
        {
            var locations = await _apiService.GetCampusLocationsAsync(universityId);
            if (locations != null && locations.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var dep = DeparturePicker.SelectedItem?.ToString();
                    var dest = DestinationPicker.SelectedItem?.ToString();

                    foreach (var loc in locations)
                    {
                        if (!DeparturePicker.Items.Contains(loc.Name))
                            DeparturePicker.Items.Add(loc.Name);
                        if (!DestinationPicker.Items.Contains(loc.Name))
                            DestinationPicker.Items.Add(loc.Name);
                    }

                    if (!string.IsNullOrEmpty(dep) && DeparturePicker.Items.Contains(dep))
                        DeparturePicker.SelectedItem = dep;
                    if (!string.IsNullOrEmpty(dest) && DestinationPicker.Items.Contains(dest))
                        DestinationPicker.SelectedItem = dest;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadLocationsForPickersAsync error: {ex.Message}");
        }
    }

    private void OnMapLocationSelected(object? sender, MapLocationSelectedEventArgs e)
    {
        SelectLocationForCurrentPicker(e.LocationName, e.Latitude, e.Longitude);
    }

    private void SelectLocationForCurrentPicker(string locationName, double? latitude = null, double? longitude = null)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool isDestination = _lastFocusedPickerName == "Destination";
            Picker targetPicker = isDestination ? DestinationPicker : DeparturePicker;

            if (isDestination)
            {
                _destinationLatitude = latitude;
                _destinationLongitude = longitude;
            }
            else
            {
                _departureLatitude = latitude;
                _departureLongitude = longitude;
            }

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
    }

    private void ShowMapView()
    {
        FormView.IsVisible = false;
        MapContainer.IsVisible = true;
        if (MapWebView.Source == null && !string.IsNullOrEmpty(_mapHtmlContent))
        {
            MapWebView.Source = new HtmlWebViewSource { Html = _mapHtmlContent };
        }
    }

    private void OnCloseMapClicked(object sender, EventArgs e)
    {
        ShowFormView();
    }

    private async void OnWomenOnlyChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingWomenOnlyAvailability || !e.Value || IsCurrentUserFemale())
        {
            return;
        }

        _isUpdatingWomenOnlyAvailability = true;
        WomenOnlyCheckBox.IsChecked = false;
        _isUpdatingWomenOnlyAvailability = false;

        await DisplayAlert("Sadece Kadınlar", "Bu seçeneği yalnızca kadın üyeler kullanabilir.", "Tamam");
    }

    private void OnShareRegisteredPhoneChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingRegisteredPhoneOption || string.IsNullOrWhiteSpace(_registeredPhoneNumber))
        {
            return;
        }

        if (e.Value)
        {
            ContactPhoneEntry.Text = _registeredPhoneNumber;
            ContactPhoneEntry.IsEnabled = false;
            return;
        }

        if (string.Equals(ContactPhoneEntry.Text?.Trim(), _registeredPhoneNumber, StringComparison.OrdinalIgnoreCase))
        {
            ContactPhoneEntry.Text = string.Empty;
        }

        ContactPhoneEntry.IsEnabled = true;
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
        var iletisimTelefonu = ContactPhoneEntry.Text?.Trim() ?? string.Empty;

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

        if (iletisimTelefonu.Length > 20)
        {
            await DisplayAlert("Telefon", "Telefon numarası en fazla 20 karakter olabilir.", "Tamam");
            return;
        }

        if (!TryGetCurrentUserId(out var surucuId))
        {
            await DisplayAlert("Oturum Hatası", "Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Tamam");
            return;
        }

        var userUniId = Preferences.Default.Get("UserUniversityId", 0);
        int? universityId = userUniId > 0 ? userUniId : (_currentMapUniversity?.Id ?? _editingRide?.UniversityId);

        var kaydedilecekYolculuk = new Yolculuk
        {
            Id = _editingRide?.Id ?? 0,
            KalkisNoktasi = kalkis,
            KalkisLatitude = _departureLatitude ?? _editingRide?.KalkisLatitude,
            KalkisLongitude = _departureLongitude ?? _editingRide?.KalkisLongitude,
            VarisNoktasi = varis,
            VarisLatitude = _destinationLatitude ?? _editingRide?.VarisLatitude,
            VarisLongitude = _destinationLongitude ?? _editingRide?.VarisLongitude,
            UniversityId = universityId,
            KalkisZamani = RideDatePicker.Date.Add(RideTimePicker.Time),
            BosKoltukSayisi = koltukSayisi,
            KisiBasiUcret = ucret,
            Aciklama = aciklama,
            IletisimTelefonu = iletisimTelefonu,
            SadeceKadinlarMi = WomenOnlyCheckBox?.IsChecked ?? false,
            SurucuId = surucuId,
            AktifMi = _editingRide?.AktifMi ?? true,
            SilindiMi = _editingRide?.SilindiMi ?? false
        };

        try
        {
            bool success = _editingRide == null
                ? await _apiService.YolculukEkleAsync(kaydedilecekYolculuk, kaydedilecekYolculuk.SurucuId)
                : await _apiService.YolculukGuncelleAsync(_editingRide.Id, kaydedilecekYolculuk, surucuId);

            if (success)
            {
                var successMessage = _editingRide == null
                    ? "Yolculuk ilanınız başarıyla yayınlandı! 🚗"
                    : "Yolculuk ilanınız başarıyla güncellendi.";
                await DisplayAlert("Başarılı", successMessage, "Harika");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Hata", "İlan kaydedilemedi. Bilgileri kontrol edip tekrar deneyin.", "Tamam");
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

    private static bool IsCurrentUserFemale()
    {
        var gender = Preferences.Default.Get("UserGender", string.Empty);
        return string.Equals(gender?.Trim(), "Kadın", StringComparison.OrdinalIgnoreCase);
    }
}
