using Microsoft.Maui.Controls;
using UI.Maui.Pages;

namespace UI.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new MainPage());
    }
}