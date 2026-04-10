using GradeTrackerApp.Services;

namespace GradeTrackerApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        try
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            

            var result = await _api.LoginAsync(
                EmailEntry.Text.Trim(),
                PasswordEntry.Text.Trim());

            if (result == null)
            {
                await DisplayAlert("Failed",
                    "Result was null - API returned error", "OK");
                return;
            }

            _api.SetToken(result.Token);
            Preferences.Set("jwt_token", result.Token);
            Preferences.Set("user_name", result.FullName);
            Preferences.Set("user_email", result.Email);
            Preferences.Set("user_program", result.Program);

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Exception",
                $"Type: {ex.GetType().Name}\nMessage: {ex.Message}\nInner: {ex.InnerException?.Message}",
                "OK");
        }
        finally
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = true;
        }
    }

    private async void OnSignUpTabClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SignUpPage(_api)); 
    }
}