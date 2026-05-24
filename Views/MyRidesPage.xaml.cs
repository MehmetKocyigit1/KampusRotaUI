using System.Collections.ObjectModel;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class MyRidesPage : ContentPage
{
    private readonly ApiServices _apiService = new();
    private readonly ObservableCollection<Yolculuk> _myPublishedRides = new();
    private readonly ObservableCollection<YolculukTalebi> _incomingRequests = new();
    private readonly ObservableCollection<YolculukTalebi> _outgoingRequests = new();

    public MyRidesPage()
    {
        InitializeComponent();
        MyPublishedRidesCollection.ItemsSource = _myPublishedRides;
        IncomingRequestsCollection.ItemsSource = _incomingRequests;
        OutgoingRequestsCollection.ItemsSource = _outgoingRequests;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async void OnRefreshRequested(object sender, EventArgs e)
    {
        await LoadDataAsync();
        RefreshContainer.IsRefreshing = false;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            await DisplayAlert("Oturum", "Yolculuklarını görmek için tekrar giriş yapmalısın.", "Tamam");
            return;
        }

        var allRides = await _apiService.TumYolculuklariGetirAsync();
        var incoming = await _apiService.SurucuTalepleriniGetirAsync(userId);
        var outgoing = await _apiService.YolcuTalepleriniGetirAsync(userId);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _myPublishedRides.Clear();
            foreach (var ride in allRides.Where(r => r.SurucuId == userId && !r.SilindiMi).OrderByDescending(r => r.KalkisZamani))
            {
                _myPublishedRides.Add(ride);
            }

            _incomingRequests.Clear();
            var orderedIncoming = incoming
                .OrderByDescending(r => r.BekliyorMu)
                .ThenByDescending(r => r.TalepTarihi)
                .ToList();

            foreach (var request in orderedIncoming)
            {
                _incomingRequests.Add(request);
            }

            _outgoingRequests.Clear();
            foreach (var request in outgoing)
            {
                _outgoingRequests.Add(request);
            }

            UpdateNotificationBadges();
        });
    }

    private async void OnApproveRequestClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: YolculukTalebi request })
        {
            return;
        }

        if (request.Durum != "Bekliyor")
        {
            await DisplayAlert("Bilgi", "Bu talep zaten sonuçlandırılmış.", "Tamam");
            return;
        }

        var success = await _apiService.TalepDurumuGuncelleAsync(request.Id, GetCurrentUserId(), true);
        if (success)
        {
            UpdateIncomingRequestLocally(request, "Onaylandı");
        }

        await DisplayAlert(success ? "Onaylandı" : "Hata", success ? "Katılım talebi onaylandı." : "Talep onaylanamadı.", "Tamam");
        await LoadDataAsync();
    }

    private async void OnRejectRequestClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: YolculukTalebi request })
        {
            return;
        }

        if (request.Durum != "Bekliyor")
        {
            await DisplayAlert("Bilgi", "Bu talep zaten sonuçlandırılmış.", "Tamam");
            return;
        }

        var success = await _apiService.TalepDurumuGuncelleAsync(request.Id, GetCurrentUserId(), false);
        if (success)
        {
            UpdateIncomingRequestLocally(request, "Reddedildi");
        }

        await DisplayAlert(success ? "Reddedildi" : "Hata", success ? "Katılım talebi reddedildi." : "Talep reddedilemedi.", "Tamam");
        await LoadDataAsync();
    }

    private void UpdateIncomingRequestLocally(YolculukTalebi request, string newStatus)
    {
        var index = _incomingRequests.IndexOf(request);
        if (index < 0)
        {
            return;
        }

        request.Durum = newStatus;
        request.OnayTarihi = DateTime.Now;
        _incomingRequests.RemoveAt(index);
        _incomingRequests.Insert(index, request);
        UpdateNotificationBadges();
    }

    private void UpdateNotificationBadges()
    {
        var pendingCount = _incomingRequests.Count(request => request.BekliyorMu);
        var hasPending = pendingCount > 0;

        PendingRequestsBadge.IsVisible = hasPending;
        IncomingPendingCountBadge.IsVisible = hasPending;

        PendingRequestsBadgeLabel.Text = pendingCount == 1
            ? "1 yeni katılım talebin var. Onaylayabilir veya reddedebilirsin."
            : $"{pendingCount} yeni katılım talebin var. Onaylayabilir veya reddedebilirsin.";

        IncomingPendingCountLabel.Text = pendingCount == 1
            ? "1 yeni"
            : $"{pendingCount} yeni";
    }

    private async void OnDeleteRideClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Yolculuk ride })
        {
            return;
        }

        var confirm = await DisplayAlert("İlanı Sil", $"{ride.Rota} ilanını silmek istiyor musun?", "Sil", "Vazgeç");
        if (!confirm)
        {
            return;
        }

        var success = await _apiService.YolculukSilAsync(ride.Id, GetCurrentUserId());
        await DisplayAlert(success ? "Silindi" : "Hata", success ? "İlan silindi." : "İlan silinemedi.", "Tamam");
        await LoadDataAsync();
    }

    private async void OnRateDriverClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: YolculukTalebi request })
        {
            return;
        }

        if (request.Durum != "Onaylandı" || request.Yolculuk == null)
        {
            await DisplayAlert("Puanlama", "Sürücüyü puanlamak için talebin onaylanmış olmalı.", "Tamam");
            return;
        }

        await RateUserAsync(request.YolculukId, request.Yolculuk.SurucuId, "Sürücüyü puanla");
    }

    private async void OnRatePassengerClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: YolculukTalebi request })
        {
            return;
        }

        if (request.Durum != "Onaylandı")
        {
            await DisplayAlert("Puanlama", "Yolcuyu puanlamak için talebi önce onaylamalısın.", "Tamam");
            return;
        }

        await RateUserAsync(request.YolculukId, request.YolcuId, "Yolcuyu puanla");
    }

    private async Task RateUserAsync(int rideId, int targetUserId, string title)
    {
        var selected = await DisplayActionSheet(title, "Vazgeç", null, "5 yıldız", "4 yıldız", "3 yıldız", "2 yıldız", "1 yıldız");
        if (selected is null || selected == "Vazgeç")
        {
            return;
        }

        var rating = int.TryParse(selected[..1], out var parsedRating) ? parsedRating : 5;
        var comment = await DisplayPromptAsync("Yorum", "Kısa bir yorum yazabilirsin.", "Kaydet", "Atla", "Yorum");
        var success = await _apiService.YolculukYorumuEkleAsync(rideId, GetCurrentUserId(), targetUserId, rating, comment ?? string.Empty);

        await DisplayAlert(success ? "Teşekkürler" : "Hata", success ? "Puan ve yorum kaydedildi." : "Puanlama kaydedilemedi. Daha önce puanlamış olabilirsin.", "Tamam");
    }

    private static int GetCurrentUserId()
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        return int.TryParse(userIdText, out var userId) ? userId : 0;
    }
}
