using GradeTrackerApp.Services;

namespace GradeTrackerApp.Views;

public partial class SignUpPage : ContentPage
{
    private readonly ApiService _api;

    public SignUpPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(FullNameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(ProgramEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ErrorLabel.Text = "Please fill in all required fields.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (PasswordEntry.Text.Length < 8)
        {
            ErrorLabel.Text = "Password must be at least 8 characters.";
            ErrorLabel.IsVisible = true;
            return;
        }

        // Save optional/extra fields to Preferences
        Preferences.Set("user_student_id",
            StudentIdEntry.Text?.Trim() ?? "");
        Preferences.Set("college",
            CollegeEntry.Text?.Trim() ?? "");
        Preferences.Set("year_level",
            YearLevelEntry.Text?.Trim() ?? "");
        Preferences.Set("semester",
            SemesterEntry.Text?.Trim() ?? "");

        var success = await _api.RegisterAsync(
            FullNameEntry.Text.Trim(),
            EmailEntry.Text.Trim(),
            ProgramEntry.Text.Trim(),
            PasswordEntry.Text);

        if (!success)
        {
            ErrorLabel.Text = "Registration failed. Email may already exist.";
            ErrorLabel.IsVisible = true;
            return;
        }

        await DisplayAlert("Success! 🎉",
            "Account created! Please log in.", "OK");
        await Navigation.PopAsync();
    }

    private async void OnLogInTabClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}