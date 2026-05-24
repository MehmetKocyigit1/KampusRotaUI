using System.Text.Json;

namespace KampusRotaUI.Services;

public class MapLocationSelectedEventArgs : EventArgs
{
    public string LocationKey { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class MapInterop
{
    private static readonly Dictionary<string, string> LocationNamesByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["iyas"] = "Iyaş",
        ["dogu-kampusu"] = "Doğu Kampüsü",
        ["bati-kampusu"] = "Batı Kampüsü",
        ["100-yil"] = "100. Yıl Yerleşkesi",
        ["erkek-yurdu"] = "Erkek Yurdu",
        ["mihrihatun-kiz-yurdu"] = "MihriHatun Kız Yurdu",
        ["sehir-merkezi"] = "Şehir Merkezi (Meydan)",
        ["otogar"] = "Otogar"
    };

    public event EventHandler<MapLocationSelectedEventArgs>? LocationSelected;

    public void SelectLocation(string jsonData)
    {
        if (TryCreateSelectionFromJson(jsonData, out var selection))
        {
            LocationSelected?.Invoke(this, selection);
        }
    }

    public static bool TryCreateSelectionFromUrl(string? url, out MapLocationSelectedEventArgs selection)
    {
        selection = new MapLocationSelectedEventArgs();

        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !IsLocationSelectionUri(uri))
        {
            return false;
        }

        var data = GetQueryValue(uri.Query, "data");
        return !string.IsNullOrWhiteSpace(data) &&
               TryCreateSelectionFromJson(Uri.UnescapeDataString(data), out selection);
    }

    public static bool TryCreateSelectionFromJson(string jsonData, out MapLocationSelectedEventArgs selection)
    {
        selection = new MapLocationSelectedEventArgs();

        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;

            var locationKey = TryGetString(root, "locationKey") ?? TryGetString(root, "id") ?? string.Empty;
            var rawLocationName = TryGetString(root, "location");
            var locationName = ResolveLocationName(locationKey, rawLocationName);
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return false;
            }

            selection = new MapLocationSelectedEventArgs
            {
                LocationKey = locationKey,
                LocationName = locationName,
                Latitude = root.GetProperty("latitude").GetDouble(),
                Longitude = root.GetProperty("longitude").GetDouble()
            };

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MapInterop Error: {ex.Message}");
            return false;
        }
    }

    private static string? GetQueryValue(string query, string key)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return pair[1];
            }
        }

        return null;
    }

    private static bool IsLocationSelectionUri(Uri uri)
    {
        if (uri.Scheme == "app" &&
            string.Equals(uri.Host, "locationSelected", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
               string.Equals(uri.Host, "kampusrota.local", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.Trim('/').Equals("locationSelected", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }

    private static string? ResolveLocationName(string? locationKey, string? rawLocationName)
    {
        if (!string.IsNullOrWhiteSpace(locationKey) &&
            LocationNamesByKey.TryGetValue(locationKey, out var canonicalName))
        {
            return canonicalName;
        }

        return NormalizeKnownLocationName(rawLocationName);
    }

    private static string? NormalizeKnownLocationName(string? locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            return null;
        }

        return LocationNamesByKey.Values.FirstOrDefault(name =>
            string.Equals(name, locationName, StringComparison.CurrentCultureIgnoreCase)) ?? locationName;
    }
}
