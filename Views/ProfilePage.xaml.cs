using System.Xml;

namespace KampusRotaUI.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Login'de kaydettiğimiz bilgileri ekrana yazıyoruz
        NameLabel.Text = Preferences.Default.Get("UserFullName", "Kullanıcı");
        EmailLabel.Text = Preferences.Default.Get("UserEmail", "");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Default.Clear();
        Application.Current.MainPage = new NavigationPage(new Login());
    }
}