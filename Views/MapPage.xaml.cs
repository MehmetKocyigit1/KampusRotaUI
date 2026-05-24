using System.Globalization;

namespace KampusRotaUI.Views;

public partial class MapPage : ContentPage
{
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
        await WaitForMapReadyAsync(TimeSpan.FromSeconds(8));
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
            JsLogLabel.Text = "map.html loaded";
        }
        catch (Exception ex)
        {
            JsLogLabel.Text = "Failed to load map.html: " + ex.Message;
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
                    JsLogLabel.Text = "map ready";
                    return true;
                }
            }
            catch { }

            await Task.Delay(300);
        }

        JsLogLabel.Text = "map not ready (timeout)";
        return false;
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
                    var logs = System.Text.Json.JsonSerializer.Deserialize<List<JsLog>>(result);
                    if (logs != null && logs.Count > 0)
                    {
                        JsLogLabel.Text = string.Join('\n', logs.Take(3).Select(l => l.message));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        catch { }
    }

    private async void StartLocationUpdates()
    {
        try
        {
            var ready = await WaitForMapReadyAsync(TimeSpan.FromSeconds(8));
            if (!ready)
            {
                JsLogLabel.Text = "Map not ready, cannot set location";
                return;
            }

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                JsLogLabel.Text = "Location permission denied";
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location != null)
            {
                var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
                var js = $"setUserLocation({lat},{lng});";
                try { await MapWebView.EvaluateJavaScriptAsync(js); } catch { JsLogLabel.Text = "JS setUserLocation error"; }
            }
        }
        catch (Exception ex)
        {
            JsLogLabel.Text = "Location error: " + ex.Message;
        }
    }

    class JsLog { public string level { get; set; } = ""; public string message { get; set; } = ""; }
}
