using Microsoft.Maui.Controls;
using UI.Maui.Pages;

namespace UI.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Create the app's main window with a NavigationPage root
        return new Window(new NavigationPage(new MainPage()));
    }
}
