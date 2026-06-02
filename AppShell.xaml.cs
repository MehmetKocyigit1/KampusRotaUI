using KampusRotaUI.Views;

namespace KampusRotaUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));
        Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
    }
}
