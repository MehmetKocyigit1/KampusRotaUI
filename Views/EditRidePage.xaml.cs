using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class EditRidePage : ContentPage
{
    private readonly IApiService _apiService = new ApiServices();
    private readonly Yolculuk _yolculuk;

    public EditRidePage(Yolculuk yolculuk)
    {
        InitializeComponent();
        _yolculuk = yolculuk;
        FillForm();
    }

    private void FillForm()
    {
        DeparturePicker.SelectedItem = _yolculuk.KalkisNoktasi;
        DestinationPicker.SelectedItem = _yolculuk.VarisNoktasi;
        RideDatePicker.Date = _yolculuk.KalkisZamani.Date;
        RideTimePicker.Time = _yolculuk.KalkisZamani.TimeOfDay;
        SeatsEntry.Text = _yolculuk.BosKoltukSayisi.ToString();
        PriceEntry.Text = _yolculuk.KisiBasiUcret.ToString("0.##");
        DescriptionEditor.Text = _yolculuk.Aciklama;
        WomenOnlyCheckBox.IsChecked = _yolculuk.SadeceKadinlarMi;
        ActiveCheckBox.IsChecked = _yolculuk.AktifMi;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        MessageLabel.IsVisible = false;

        var button = (Button)sender;
        button.IsEnabled = false;

        try
        {
            string? kalkis = DeparturePicker.SelectedItem?.ToString();
            string? varis = DestinationPicker.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(kalkis) || string.IsNullOrWhiteSpace(varis))
            {
                ShowMessage("Kalkış ve varış noktası seçilmelidir.");
                return;
            }

            if (kalkis == varis)
            {
                ShowMessage("Kalkış ve varış noktası aynı olamaz.");
                return;
            }

            if (!int.TryParse(SeatsEntry.Text, out int koltukSayisi) || koltukSayisi < 1 || koltukSayisi > 8)
            {
                ShowMessage("Koltuk sayısı 1 ile 8 arasında olmalıdır.");
                return;
            }

            if (!decimal.TryParse(PriceEntry.Text, out decimal ucret) || ucret < 0)
            {
                ShowMessage("Ücret boşsa 0 yazın; negatif olamaz.");
                return;
            }

            var kalkisZamani = RideDatePicker.Date.Add(RideTimePicker.Time);
            if (kalkisZamani < DateTime.Now)
            {
                ShowMessage("Geçmiş tarihli ilan kaydedilemez.");
                return;
            }

            int userId = Preferences.Default.Get("UserId", 0);
            if (userId == 0)
            {
                ShowMessage("Oturum bulunamadı. Lütfen tekrar giriş yapın.");
                return;
            }

            _yolculuk.KalkisNoktasi = kalkis;
            _yolculuk.VarisNoktasi = varis;
            _yolculuk.KalkisZamani = kalkisZamani;
            _yolculuk.BosKoltukSayisi = koltukSayisi;
            _yolculuk.KisiBasiUcret = ucret;
            _yolculuk.Aciklama = DescriptionEditor.Text?.Trim() ?? string.Empty;
            _yolculuk.SadeceKadinlarMi = WomenOnlyCheckBox.IsChecked;
            _yolculuk.AktifMi = ActiveCheckBox.IsChecked;

            bool success = await _apiService.YolculukGuncelleAsync(_yolculuk, userId);
            if (!success)
            {
                ShowMessage("İlan güncellenemedi. Bu ilan size ait olmayabilir.");
                return;
            }

            await DisplayAlert("Başarılı", "İlan güncellendi.", "Tamam");
            await Navigation.PopAsync();
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void ShowMessage(string message)
    {
        MessageLabel.Text = message;
        MessageLabel.IsVisible = true;
    }
}
