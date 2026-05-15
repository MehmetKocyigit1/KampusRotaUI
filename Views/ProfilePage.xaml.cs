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
        // Sayfa her açıldığında kayıtlı resmi yükle
        LoadSavedProfileImage();
    }

    // --- GEMINI (ANTIGRAVITY) BUTON OLAYI ---
    private async void OnGeminiAskClicked(object sender, EventArgs e)
    {
        var soru = GeminiEntry.Text;

        if (string.IsNullOrWhiteSpace(soru))
        {
            await DisplayAlert("Uyarı", "Lütfen asistanımıza bir soru yöneltin.", "Tamam");
            return;
        }

        // Arayüzü "Düşünüyor..." moduna sokalım
        LoadingIndicator.IsRunning = true;
        GeminiResponseLabel.Text = "Antigravity sistemi kontrol ediyor...";
        GeminiEntry.IsEnabled = false;

        try
        {
            // ApiServices içindeki AGENT.md destekli metodumuzu çağırıyoruz
            var cevap = await _apiServices.AskGeminiAsync(soru);

            // Gelen cevabı ekrana yazdırıyoruz
            GeminiResponseLabel.Text = cevap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("GEMINI UI HATASI: " + ex.Message);
            GeminiResponseLabel.Text = "Üzgünüm, şu an bağlantı kuramıyorum.";
        }
        finally
        {
            // Arayüzü eski haline getir
            LoadingIndicator.IsRunning = false;
            GeminiEntry.IsEnabled = true;
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
        }
        else
        {
            string gender = Preferences.Default.Get("UserGender", "Belirtmek İstemiyorum");
            ProfileImage.Source = gender switch
            {
                "Kadın" => "insta_female.png",
                "Erkek" => "insta_male.png",
                _ => "insta_female.png"
            };
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Default.Remove("UserId");
        Preferences.Default.Remove("UserFullName");
        Preferences.Default.Remove("UserEmail");
        Preferences.Default.Remove("UserGender");

        Application.Current.MainPage = new NavigationPage(new Login());
    }

    async void OnAddPhotoClicked(object sender, EventArgs e)
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

    async Task LoadPhotoAsync(FileResult photo)
    {
        if (photo == null) return;

        string userId = Preferences.Default.Get("UserId", string.Empty);
        string fileName = $"profile_{userId}.png";
        string localFilePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        using (Stream sourceStream = await photo.OpenReadAsync())
        using (FileStream localStream = File.OpenWrite(localFilePath))
        {
            await sourceStream.CopyToAsync(localStream);
        }

        Preferences.Default.Set($"ProfilePicturePath_{userId}", localFilePath);

        var stream = await photo.OpenReadAsync();
        ProfileImage.Source = ImageSource.FromStream(() => stream);
    }
}