using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class MapPage : ContentPage
{
    private readonly ApiServices _apiServices = new();
    private List<University> _universities = new();
    private bool _isMapReady = false;

    public MapPage()
    {
        InitializeComponent();

        Dispatcher.Dispatch(async () =>
        {
            await InitializeMapAsync();
        });

        Dispatcher.StartTimer(TimeSpan.FromSeconds(1.5), () =>
        {
            _ = FetchJsLogs();
            return true;
        });
    }

    private async Task InitializeMapAsync()
    {
        await LoadMapHtmlAsync();
        _isMapReady = await WaitForMapReadyAsync(TimeSpan.FromSeconds(8));
        
        await LoadUniversitiesAsync();
        StartLocationUpdates();
    }

    private async Task LoadMapHtmlAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new System.IO.StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            MapWebView.Source = new HtmlWebViewSource { Html = html };
            JsLogLabel.Text = "Harita yüklendi";
        }
        catch (Exception ex)
        {
            JsLogLabel.Text = "Harita yüklenemedi: " + ex.Message;
        }
    }

    private async Task<bool> WaitForMapReadyAsync(TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var res = await MapWebView.EvaluateJavaScriptAsync("(function(){return window.mapReady===true;})()");
                if (!string.IsNullOrWhiteSpace(res) && (res.Trim().ToLower().Contains("true")))
                {
                    JsLogLabel.Text = "Harita hazır";
                    return true;
                }
            }
            catch { }

            await Task.Delay(300);
        }

        JsLogLabel.Text = "Harita zaman aşımı";
        return false;
    }

    private async Task LoadUniversitiesAsync()
    {
        try
        {
            _universities = await _apiServices.GetUniversitiesAsync();
            if (_universities != null && _universities.Count > 0)
            {
                UniversityPicker.ItemsSource = _universities;
                var userUniId = Preferences.Default.Get("UserUniversityId", 0);
                var targetUni = _universities.FirstOrDefault(u => u.Id == userUniId) ?? _universities[0];
                UniversityPicker.SelectedItem = targetUni;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Üniversite yükleme hatası: {ex.Message}");
        }
    }

    private async void OnUniversitySelectedIndexChanged(object sender, EventArgs e)
    {
        if (UniversityPicker.SelectedItem is not University selectedUni)
            return;

        if (!_isMapReady)
        {
            _isMapReady = await WaitForMapReadyAsync(TimeSpan.FromSeconds(3));
            if (!_isMapReady) return;
        }

        try
        {
            // First call selectUniversityById for instant response from built-in 180+ POI catalog
            await MapWebView.EvaluateJavaScriptAsync($"if(window.selectUniversityById) {{ window.selectUniversityById({selectedUni.Id}); }}");

            // 1. Focus map camera to the selected university campus
            var latStr = selectedUni.Latitude.ToString(CultureInfo.InvariantCulture);
            var lngStr = selectedUni.Longitude.ToString(CultureInfo.InvariantCulture);
            await MapWebView.EvaluateJavaScriptAsync($"focusUniversity({latStr}, {lngStr}, {selectedUni.DefaultZoom});");

            // 2. Fetch and render campus place pins for this university
            var locations = await _apiServices.GetCampusLocationsAsync(selectedUni.Id);
            if (locations != null && locations.Count > 0)
            {
                var json = JsonSerializer.Serialize(locations);
                var escapedJson = JsonSerializer.Serialize(json); // Escape for JavaScript argument
                await MapWebView.EvaluateJavaScriptAsync($"loadCampusPlaces({escapedJson});");
            }

            JsLogLabel.Text = $"{selectedUni.Name} odaklandı ({locations?.Count ?? 0} durak)";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Üniversite değiştirme hatası: {ex.Message}");
        }
    }

    private async Task FetchJsLogs()
    {
        try
        {
            var result = await MapWebView.EvaluateJavaScriptAsync("(function(){return JSON.stringify(getAndClearLogs());})()");
            if (!string.IsNullOrWhiteSpace(result) && result != "null")
            {
                try
                {
                    var logs = JsonSerializer.Deserialize<List<JsLog>>(result);
                    if (logs != null && logs.Count > 0)
                    {
                        JsLogLabel.Text = string.Join('\n', logs.Take(2).Select(l => l.message));
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private async void StartLocationUpdates()
    {
        try
        {
            if (!_isMapReady) return;

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                JsLogLabel.Text = "Konum izni verilmedi";
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location != null)
            {
                var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
                var js = $"setUserLocation({lat},{lng});";
                try { await MapWebView.EvaluateJavaScriptAsync(js); } catch { }
            }
        }
        catch (Exception ex)
        {
            JsLogLabel.Text = "Konum hatası: " + ex.Message;
        }
    }

    class JsLog { public string level { get; set; } = ""; public string message { get; set; } = ""; }
}
