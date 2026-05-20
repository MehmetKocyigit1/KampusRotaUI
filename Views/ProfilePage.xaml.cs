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

        // Load saved name/email/bio if available
        NameLabel.Text = Preferences.Default.Get("UserFullName", "Kullanıcı");
        EmailLabel.Text = Preferences.Default.Get("UserEmail", string.Empty);

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

    // New event handlers referenced in XAML
    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChangePasswordPage());
    }

    private async void OnAddRideClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddRidePage());
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        // Simple modal to edit bio
        var editor = new Editor { Text = Preferences.Default.Get("UserBio", string.Empty), HeightRequest = 120 };
        var saveButton = new Button { Text = "Kaydet", BackgroundColor = Color.FromArgb("#512BD4"), TextColor = Colors.White };
        var cancelButton = new Button { Text = "İptal", BackgroundColor = Colors.Transparent, TextColor = Colors.Black };

        var layout = new VerticalStackLayout { Padding = 18, Spacing = 12 };
        layout.Add(new Label { Text = "Profil Biyografi", FontAttributes = FontAttributes.Bold, FontSize = 18 });
        layout.Add(editor);
        var btnRow = new HorizontalStackLayout { Spacing = 10 };
        btnRow.Add(saveButton);
        btnRow.Add(cancelButton);
        layout.Add(btnRow);

        var modal = new ContentPage { Content = layout };

        saveButton.Clicked += async (_, _) =>
        {
            Preferences.Default.Set("UserBio", editor.Text ?? string.Empty);
            await Navigation.PopModalAsync();
            BioLabel.Text = editor.Text;
            BioLabel.IsVisible = !string.IsNullOrWhiteSpace(editor.Text);
        };

        cancelButton.Clicked += async (_, _) =>
        {
            await Navigation.PopModalAsync();
        };

        await Navigation.PushModalAsync(modal);
    }
}