using KampusRotaUI.Models;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class EditProfilePage : ContentPage
{
    private readonly ApiServices _apiServices = new();
    private Kullanici? _currentUser;

    public EditProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadLocalProfilePhoto();
        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            ShowMessage("Oturum bilgisi bulunamadı. Lütfen tekrar giriş yap.", Colors.Red);
            return;
        }

        _currentUser = await _apiServices.KullaniciGetirAsync(userId);
        if (_currentUser == null)
        {
            ShowMessage("Profil bilgileri alınamadı.", Colors.Red);
            return;
        }

        NameEntry.Text = _currentUser.Ad;
        SurnameEntry.Text = _currentUser.Soyad;
        PhoneEntry.Text = _currentUser.TelefonNumarasi;
        BioEditor.Text = _currentUser.Biyografi;
        EmailValueLabel.Text = _currentUser.Email;
        StudentNoValueLabel.Text = _currentUser.OgrenciNumarasi;
        PreviewEmailLabel.Text = _currentUser.Email;
        SetGender(_currentUser.Cinsiyet);
        RefreshPreviewName();
        UpdateBioCounter();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_currentUser == null)
        {
            ShowMessage("Profil bilgileri henüz yüklenmedi.", Colors.Red);
            return;
        }

        var ad = NameEntry.Text?.Trim() ?? string.Empty;
        var soyad = SurnameEntry.Text?.Trim() ?? string.Empty;
        var phone = PhoneEntry.Text?.Trim() ?? string.Empty;
        var bio = BioEditor.Text?.Trim() ?? string.Empty;
        var gender = GenderPicker.SelectedItem?.ToString() ?? "Belirtmek İstemiyorum";

        if (ad.Length is < 2 or > 40 || soyad.Length is < 2 or > 40)
        {
            ShowMessage("Ad ve soyad 2-40 karakter arasında olmalı.", Colors.Red);
            return;
        }

        if (phone.Length > 20)
        {
            ShowMessage("Telefon numarası en fazla 20 karakter olabilir.", Colors.Red);
            return;
        }

        if (bio.Length > 250)
        {
            ShowMessage("Biyografi en fazla 250 karakter olabilir.", Colors.Red);
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Text = "Kaydediliyor";

        var updatedUser = new Kullanici
        {
            Ad = ad,
            Soyad = soyad,
            TelefonNumarasi = phone,
            Cinsiyet = gender,
            Biyografi = bio,
            ProfilFotografiUrl = _currentUser.ProfilFotografiUrl
        };

        var savedUser = await _apiServices.ProfilGuncelleAsync(_currentUser.Id, updatedUser);

        SaveButton.IsEnabled = true;
        SaveButton.Text = "Kaydet";

        if (savedUser == null)
        {
            ShowMessage("Profil kaydedilemedi. Bağlantını kontrol edip tekrar dene.", Colors.Red);
            return;
        }

        _currentUser = savedUser;
        SaveUserPreferences(savedUser);
        ShowMessage("Profil güncellendi.", Color.FromArgb("#15803D"));

        await Task.Delay(450);
        await GoBackAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await GoBackAsync();
    }

    private async void OnChangePhotoClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("Profil Fotoğrafı", "Vazgeç", null, "Fotoğraf Çek", "Galeriden Seç");

        FileResult? photo = null;
        if (action == "Fotoğraf Çek")
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ShowMessage("Bu cihazda kamera kullanılamıyor.", Colors.Red);
                return;
            }

            photo = await MediaPicker.Default.CapturePhotoAsync();
        }
        else if (action == "Galeriden Seç")
        {
            photo = await MediaPicker.Default.PickPhotoAsync();
        }

        await LoadPhotoAsync(photo);
    }

    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        var userId = Preferences.Default.Get("UserId", string.Empty);
        var path = Preferences.Default.Get($"ProfilePicturePath_{userId}", string.Empty);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        Preferences.Default.Remove($"ProfilePicturePath_{userId}");
        LoadLocalProfilePhoto();
        ShowMessage("Profil fotoğrafı kaldırıldı.", Color.FromArgb("#15803D"));
    }

    private void OnNameTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshPreviewName();
    }

    private void OnBioTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateBioCounter();
    }

    private async Task LoadPhotoAsync(FileResult? photo)
    {
        if (photo == null)
        {
            return;
        }

        var userId = Preferences.Default.Get("UserId", string.Empty);
        var fileName = $"profile_{userId}.png";
        var localFilePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        using (var sourceStream = await photo.OpenReadAsync())
        using (var localStream = File.Create(localFilePath))
        {
            await sourceStream.CopyToAsync(localStream);
        }

        Preferences.Default.Set($"ProfilePicturePath_{userId}", localFilePath);
        ProfilePreviewImage.Source = ImageSource.FromFile(localFilePath);
        ShowMessage("Profil fotoğrafı güncellendi.", Color.FromArgb("#15803D"));
    }

    private void LoadLocalProfilePhoto()
    {
        var userId = Preferences.Default.Get("UserId", string.Empty);
        var savedPath = Preferences.Default.Get($"ProfilePicturePath_{userId}", string.Empty);

        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
        {
            ProfilePreviewImage.Source = ImageSource.FromFile(savedPath);
            return;
        }

        var gender = Preferences.Default.Get("UserGender", "Belirtmek İstemiyorum");
        ProfilePreviewImage.Source = gender == "Erkek" ? "insta_male.png" : "insta_female.png";
    }

    private void RefreshPreviewName()
    {
        var fullName = $"{NameEntry.Text?.Trim()} {SurnameEntry.Text?.Trim()}".Trim();
        PreviewNameLabel.Text = string.IsNullOrWhiteSpace(fullName) ? "Kullanıcı" : fullName;
    }

    private void UpdateBioCounter()
    {
        var length = BioEditor.Text?.Length ?? 0;
        BioCounterLabel.Text = $"{length}/250";
        BioCounterLabel.TextColor = length > 250 ? Colors.Red : Color.FromArgb("#74777D");
    }

    private void SetGender(string? gender)
    {
        var value = string.IsNullOrWhiteSpace(gender) ? "Belirtmek İstemiyorum" : gender;
        var index = GenderPicker.Items.IndexOf(value);
        GenderPicker.SelectedIndex = index >= 0 ? index : 2;
    }

    private void ShowMessage(string message, Color color)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = color;
        MessageLabel.IsVisible = true;
    }

    private static void SaveUserPreferences(Kullanici user)
    {
        Preferences.Default.Set("UserFullName", user.TamAd);
        Preferences.Default.Set("UserEmail", user.Email ?? string.Empty);
        Preferences.Default.Set("UserGender", user.Cinsiyet ?? string.Empty);
        Preferences.Default.Set("UserPhone", user.TelefonNumarasi ?? string.Empty);
        Preferences.Default.Set("UserStudentNo", user.OgrenciNumarasi ?? string.Empty);
        Preferences.Default.Set("UserBio", user.Biyografi ?? string.Empty);
        Preferences.Default.Set("UserProfilePhotoUrl", user.ProfilFotografiUrl ?? string.Empty);
    }

    private async Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Navigation.PopAsync();
    }

    private static int GetCurrentUserId()
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        return int.TryParse(userIdText, out var userId) ? userId : 0;
    }
}
