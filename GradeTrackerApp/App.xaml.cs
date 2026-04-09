using GradeTrackerApp.Services;
using GradeTrackerApp.Views;

namespace GradeTrackerApp;

public partial class App : Application
{
    public App(ApiService api)
    {
        InitializeComponent();

        try
        {
            var token = Preferences.Get("jwt_token", string.Empty);

            // Restore token to ApiService
            if (!string.IsNullOrEmpty(token))
            {
                api.SetToken(token);      // ← add this line
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage(api));
            }
        }
        catch (Exception ex)
        {
            MainPage = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Startup Error:\n{ex.Message}",
                    Margin = new Thickness(20),
                    TextColor = Colors.Red,
                    VerticalOptions = LayoutOptions.Center
                }
            };
        }
    }
}