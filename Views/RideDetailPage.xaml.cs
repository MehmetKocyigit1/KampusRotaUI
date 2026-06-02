using System.Diagnostics;
using System.Globalization;
using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class RideDetailPage : ContentPage
{
    private readonly ApiServices _apiService = new();
    private readonly Yolculuk _ride;

    public RideDetailPage(Yolculuk ride)
    {
        InitializeComponent();
        _ride = ride;
        RenderRide();
    }

    private void RenderRide()
    {
        RouteLabel.Text = _ride.Rota;
        DateLabel.Text = _ride.TarihFormatli;
        PriceLabel.Text = _ride.UcretMetni;
        SeatLabel.Text = $"{_ride.BosKoltukSayisi} koltuk";
        WomenOnlyBadge.IsVisible = _ride.SadeceKadinlarMi;

        var driverName = _ride.Surucu?.TamAd?.Trim();
        DriverNameLabel.Text = string.IsNullOrWhiteSpace(driverName) ? "Sürücü bilgisi" : driverName;

        var rating = _ride.Surucu?.OrtalamaPuan ?? 5.0;
        DriverRatingLabel.Text = rating.ToString("0.0", CultureInfo.InvariantCulture);
        DriverStarsLabel.Text = BuildStars(rating);

        var email = GetSchoolEmail();
        var phone = GetContactPhone();
        DriverEmailLabel.Text = string.IsNullOrWhiteSpace(email) ? "Okul e-postası: belirtilmemiş" : $"Okul e-postası: {email}";
        DriverPhoneLabel.Text = string.IsNullOrWhiteSpace(phone) ? "Telefon: belirtilmemiş" : $"Telefon: {phone}";

        DescriptionBlock.IsVisible = !string.IsNullOrWhiteSpace(_ride.Aciklama);
        DescriptionLabel.Text = _ride.Aciklama;

        JoinButton.IsEnabled = _ride.BosKoltukSayisi > 0 && _ride.KalkisZamani > DateTime.Now;
        JoinButton.Text = JoinButton.IsEnabled ? "Katılma talebi gönder" : "Bu ilan uygun değil";
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnJoinClicked(object sender, EventArgs e)
    {
        if (_ride.BosKoltukSayisi <= 0)
        {
            await DisplayAlert("Koltuk Yok", "Bu ilanda boş koltuk kalmamış.", "Tamam");
            return;
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0)
        {
            await DisplayAlert("Oturum", "Katılmak için tekrar giriş yapmalısın.", "Tamam");
            return;
        }

        if (_ride.SurucuId == currentUserId)
        {
            await DisplayAlert("Bilgi", "Kendi ilanına katılma talebi gönderemezsin.", "Tamam");
            return;
        }

        var confirm = await DisplayAlert(
            "Katılma Talebi",
            $"{_ride.Rota} yolculuğu için sürücüye talep göndermek istiyor musun?",
            "Gönder",
            "Vazgeç");

        if (!confirm)
        {
            return;
        }

        try
        {
            var success = await _apiService.KatilmaTalebiGonderAsync(
                _ride.Id,
                currentUserId,
                $"{_ride.Rota} yolculuğuna katılmak istiyorum.");
            if (!success)
            {
                await DisplayAlert("Hata", "Talep gönderilemedi. Lütfen tekrar dene.", "Tamam");
                return;
            }

            JoinButton.IsEnabled = false;
            JoinButton.Text = "Talep gönderildi";

            await DisplayAlert(
                "Talep Gönderildi",
                "Talebin sürücüye iletildi. Onay durumunu Yolculuklarım sayfasından takip edebilirsin.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Katılma talebi hatası: {ex.Message}");
            await DisplayAlert("Hata", "Talep gönderilirken bir sorun oluştu.", "Tamam");
        }
    }

    private async void OnCallClicked(object sender, EventArgs e)
    {
        var phone = GetContactPhone();
        if (string.IsNullOrWhiteSpace(phone))
        {
            await DisplayAlert("Telefon", "Sürücü bu ilan için telefon numarası paylaşmamış.", "Tamam");
            return;
        }

        await Launcher.Default.OpenAsync($"tel:{Uri.EscapeDataString(phone)}");
    }

    private async void OnEmailClicked(object sender, EventArgs e)
    {
        var email = GetSchoolEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Okul E-postası", "Sürücü okul e-postası paylaşmamış.", "Tamam");
            return;
        }

        var subject = Uri.EscapeDataString($"KampüsRota yolculuk talebi: {_ride.Rota}");
        var body = Uri.EscapeDataString("Merhaba, ilandaki yolculuğa katılmak istiyorum. Uygunsa detayları konuşabilir miyiz?");
        await Launcher.Default.OpenAsync($"mailto:{email}?subject={subject}&body={body}");
    }

    private string GetContactPhone()
    {
        return !string.IsNullOrWhiteSpace(_ride.IletisimTelefonu)
            ? _ride.IletisimTelefonu.Trim()
            : _ride.Surucu?.TelefonNumarasi?.Trim() ?? string.Empty;
    }

    private string GetSchoolEmail()
    {
        return _ride.Surucu?.Email?.Trim() ?? string.Empty;
    }

    private static int GetCurrentUserId()
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        return int.TryParse(userIdText, out var userId) ? userId : 0;
    }

    private static string BuildStars(double rating)
    {
        var fullStars = Math.Clamp((int)Math.Round(rating, MidpointRounding.AwayFromZero), 0, 5);
        return new string('★', fullStars) + new string('☆', 5 - fullStars);
    }
}
