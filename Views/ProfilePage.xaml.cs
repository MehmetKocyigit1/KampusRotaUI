using System.Diagnostics;
using KampusRotaUI.Services;

namespace KampusRotaUI.Views;

public partial class ProfilePage : ContentPage
{
    // Gemini servisimizi başlatıyoruz
    private readonly ApiServices _apiServices = new ApiServices();

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadProfileDetails();
        _ = RefreshProfileAsync();
        _ = LoadProfileStatsAsync();
    }

    private void LoadProfileDetails()
    {
        LoadSavedProfileImage();

        NameLabel.Text = Preferences.Default.Get("UserFullName", "Kullanıcı");
        EmailLabel.Text = Preferences.Default.Get("UserEmail", string.Empty);
        PhoneSummaryLabel.Text = GetDisplayValue(Preferences.Default.Get("UserPhone", string.Empty));
        GenderSummaryLabel.Text = GetDisplayValue(Preferences.Default.Get("UserGender", string.Empty));
        StudentNoSummaryLabel.Text = GetDisplayValue(Preferences.Default.Get("UserStudentNo", string.Empty));

        var bio = Preferences.Default.Get("UserBio", string.Empty);
        if (!string.IsNullOrEmpty(bio))
        {
            BioLabel.Text = bio;
            BioLabel.IsVisible = true;
        }
        else
        {
            BioLabel.IsVisible = false;
        }

        UpdateProfileCompletion();
    }

    private async Task RefreshProfileAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return;
        }

        try
        {
            var currentUser = await _apiServices.KullaniciGetirAsync(userId);
            if (currentUser == null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Preferences.Default.Set("UserFullName", currentUser.TamAd);
                Preferences.Default.Set("UserEmail", currentUser.Email ?? string.Empty);
                Preferences.Default.Set("UserGender", currentUser.Cinsiyet ?? string.Empty);
                Preferences.Default.Set("UserPhone", currentUser.TelefonNumarasi ?? string.Empty);
                Preferences.Default.Set("UserStudentNo", currentUser.OgrenciNumarasi ?? string.Empty);
                Preferences.Default.Set("UserBio", currentUser.Biyografi ?? string.Empty);
                Preferences.Default.Set("UserProfilePhotoUrl", currentUser.ProfilFotografiUrl ?? string.Empty);
                LoadProfileDetails();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profil bilgileri yenilenemedi: {ex.Message}");
        }
    }

    private async Task LoadProfileStatsAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            RideCountLabel.Text = "0";
            RatingLabel.Text = "5.0";
            RatingStarsLabel.Text = BuildStars(5.0);
            return;
        }

        try
        {
            var currentUser = await _apiServices.KullaniciGetirAsync(userId);
            var rides = await _apiServices.TumYolculuklariGetirAsync();
            var myRides = rides
                .Where(r => r.SurucuId == userId && !r.SilindiMi)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var ratingValue = currentUser?.OrtalamaPuan ?? 5.0;
                RideCountLabel.Text = myRides.Count.ToString();
                RatingLabel.Text = ratingValue.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                RatingStarsLabel.Text = BuildStars(ratingValue);
                Preferences.Default.Set("UserRating", RatingLabel.Text);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profil istatistikleri yüklenemedi: {ex.Message}");
        }
    }

    // --- PROFİL FOTOĞRAFI YÖNETİMİ ---

    private void LoadSavedProfileImage()
    {
        string userId = Preferences.Default.Get("UserId", string.Empty);
        string savedPath = Preferences.Default.Get($"ProfilePicturePath_{userId}", string.Empty);

        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
        {
            ProfileImage.Source = ImageSource.FromFile(savedPath);
            HeaderProfileImage.Source = ImageSource.FromFile(savedPath);
        }
        else
        {
            string gender = Preferences.Default.Get("UserGender", "Belirtmek İstemiyorum");
            var defaultImage = gender switch
            {
                "Kadın" => "insta_female.png",
                "Erkek" => "insta_male.png",
                _ => "insta_female.png"
            };
            ProfileImage.Source = defaultImage;
            HeaderProfileImage.Source = defaultImage;
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        ClearSessionPreferences();
        NavigateToLogin();
    }

    private async void OnAddPhotoTapped(object sender, TappedEventArgs e)
    {
        await ManageProfilePhotoAsync();
    }

    async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        await ManageProfilePhotoAsync();
    }

    private async Task ManageProfilePhotoAsync()
    {
        string userId = Preferences.Default.Get("UserId", string.Empty);
        string savedPath = Preferences.Default.Get($"ProfilePicturePath_{userId}", string.Empty);
        bool hasPhoto = !string.IsNullOrEmpty(savedPath) && File.Exists(savedPath);

        string action;
        if (hasPhoto)
        {
            action = await DisplayActionSheet("Profil Fotoğrafını Yönet", "Vazgeç", "Fotoğrafı Kaldır", "Yeni Fotoğraf Çek", "Galeriden Seç");
        }
        else
        {
            action = await DisplayActionSheet("Profil Fotoğrafı Ekle", "Vazgeç", null, "Fotoğraf Çek", "Galeriden Seç");
        }

        if (action == "Fotoğrafı Kaldır")
        {
            RemoveProfilePhoto(userId, savedPath);
        }
        else if (action == "Fotoğraf Çek" || action == "Yeni Fotoğraf Çek")
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                await LoadPhotoAsync(photo);
            }
        }
        else if (action == "Galeriden Seç")
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            await LoadPhotoAsync(photo);
        }
    }

    private void RemoveProfilePhoto(string userId, string path)
    {
        if (File.Exists(path)) File.Delete(path);
        Preferences.Default.Remove($"ProfilePicturePath_{userId}");
        LoadSavedProfileImage();
    }

    async Task LoadPhotoAsync(FileResult? photo)
    {
        if (photo == null) return;

        string userId = Preferences.Default.Get("UserId", string.Empty);
        string fileName = $"profile_{userId}.png";
        string localFilePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        using (Stream sourceStream = await photo.OpenReadAsync())
        using (FileStream localStream = File.Create(localFilePath))
        {
            await sourceStream.CopyToAsync(localStream);
        }

        Preferences.Default.Set($"ProfilePicturePath_{userId}", localFilePath);

        ProfileImage.Source = ImageSource.FromFile(localFilePath);
        HeaderProfileImage.Source = ImageSource.FromFile(localFilePath);
    }

    // New event handlers referenced in XAML
    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync(nameof(ChangePasswordPage));
            return;
        }

        await Navigation.PushAsync(new ChangePasswordPage());
    }

    private async void OnAddRideClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("//AddRidePage");
        }
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync(nameof(EditProfilePage));
            return;
        }

        await Navigation.PushAsync(new EditProfilePage());
    }

    private async void OnNotificationSettingsClicked(object sender, EventArgs e)
    {
        var notificationsEnabled = Preferences.Default.Get("NotificationsEnabled", true);
        var action = await DisplayActionSheet(
            "Bildirim Ayarları",
            "Vazgeç",
            null,
            notificationsEnabled ? "Bildirimleri Kapat" : "Bildirimleri Aç");

        if (action == "Bildirimleri Aç")
        {
            Preferences.Default.Set("NotificationsEnabled", true);
            await DisplayAlert("Bildirimler", "Bildirimler açıldı.", "Tamam");
        }
        else if (action == "Bildirimleri Kapat")
        {
            Preferences.Default.Set("NotificationsEnabled", false);
            await DisplayAlert("Bildirimler", "Bildirimler kapatıldı.", "Tamam");
        }
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        const string supportEmail = "hmkcygt4@gmail.com";

        try
        {
            var subject = Uri.EscapeDataString("KampüsRota Yardım ve Destek");
            var body = Uri.EscapeDataString("Merhaba, KampüsRota uygulaması hakkında destek almak istiyorum.");
            var url = $"https://mail.google.com/mail/?view=cm&fs=1&to={supportEmail}&su={subject}&body={body}";

            await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Destek maili açılamadı: {ex.Message}");
            await Clipboard.Default.SetTextAsync(supportEmail);
            await DisplayAlert(
                "Yardım ve Destek",
                $"Destek adresi panoya kopyalandı: {supportEmail}",
                "Tamam");
        }
    }

    private async void OnDeleteAccountClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Hesabı Sil",
            "Hesabın ve aktif oturumun silinecek. Bu işlemi onaylıyor musun?",
            "Sil",
            "Vazgeç");

        if (!confirm)
        {
            return;
        }

        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            ClearSessionPreferences();
            NavigateToLogin();
            return;
        }

        var deleted = await _apiServices.KullaniciSilAsync(userId);
        if (!deleted)
        {
            await DisplayAlert("Hata", "Hesap silinemedi. Bağlantını kontrol edip tekrar dene.", "Tamam");
            return;
        }

        ClearSessionPreferences();
        await DisplayAlert("Hesap Silindi", "Hesabın başarıyla silindi.", "Tamam");
        NavigateToLogin();
    }

    private static int GetCurrentUserId()
    {
        var userIdText = Preferences.Default.Get("UserId", "0");
        return int.TryParse(userIdText, out var userId) ? userId : 0;
    }

    private static void ClearSessionPreferences()
    {
        Preferences.Default.Remove("UserId");
        Preferences.Default.Remove("UserFullName");
        Preferences.Default.Remove("UserEmail");
        Preferences.Default.Remove("UserGender");
        Preferences.Default.Remove("UserPhone");
        Preferences.Default.Remove("UserStudentNo");
        Preferences.Default.Remove("UserRating");
        Preferences.Default.Remove("UserBio");
        Preferences.Default.Remove("UserProfilePhotoUrl");
    }

    private static void NavigateToLogin()
    {
        if (Application.Current is not null)
        {
            Application.Current.MainPage = new NavigationPage(new Login());
        }
    }

    private static string BuildStars(double rating)
    {
        var fullStars = Math.Clamp((int)Math.Round(rating, MidpointRounding.AwayFromZero), 0, 5);
        return new string('★', fullStars) + new string('☆', 5 - fullStars);
    }

    private void UpdateProfileCompletion()
    {
        var completed = 0;
        var total = 5;

        if (!string.IsNullOrWhiteSpace(Preferences.Default.Get("UserFullName", string.Empty))) completed++;
        if (!string.IsNullOrWhiteSpace(Preferences.Default.Get("UserEmail", string.Empty))) completed++;
        if (!string.IsNullOrWhiteSpace(Preferences.Default.Get("UserStudentNo", string.Empty))) completed++;
        if (!string.IsNullOrWhiteSpace(Preferences.Default.Get("UserPhone", string.Empty))) completed++;
        if (!string.IsNullOrWhiteSpace(Preferences.Default.Get("UserBio", string.Empty))) completed++;

        var progress = completed / (double)total;
        ProfileCompletionBar.Progress = progress;
        ProfileCompletionLabel.Text = $"{Math.Round(progress * 100):0}%";
    }

    private static string GetDisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Belirtilmemiş" : value;
    }
}
