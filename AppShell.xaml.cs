using KampusRotaUI.Views;

namespace KampusRotaUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(EditRidePage), typeof(EditRidePage));
    }
}
