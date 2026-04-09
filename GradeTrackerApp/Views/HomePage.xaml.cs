using GradeTrackerApp.Services;

namespace GradeTrackerApp.Views;

public partial class HomePage : ContentPage
{
    private readonly ApiService _api;

    public HomePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadUserInfo();
        await LoadSubjectsAsync();
    }

    private void LoadUserInfo()
    {
        // Load saved user info
        var name = Preferences.Get("user_name", "Student");
        UserNameLabel.Text = $"{name} 👋";

        // Set avatar initials
        var initials = string.Join("", name.Split(' ')
            .Take(2).Select(w => w[0].ToString().ToUpper()));
        AvatarLabel.Text = initials;

        // Set greeting based on time
        var hour = DateTime.Now.Hour;
        GreetingLabel.Text = hour < 12 ? "Good morning," :
                             hour < 18 ? "Good afternoon," : "Good evening,";
    }

    private async Task LoadSubjectsAsync()
    {
        var subjects = await _api.GetSubjectsAsync();
        SubjectsCollection.ItemsSource = subjects;

        // Show empty state if no subjects
        HomeEmptyState.IsVisible = !subjects.Any();
        SubjectsCollection.IsVisible = subjects.Any();

        // Calculate total units
        var totalUnits = subjects.Sum(s => s.Units);
        UnitsLabel.Text = totalUnits.ToString();

        // Calculate GWA from graded subjects
        var graded = subjects.Where(s => s.Grade.HasValue).ToList();
        if (graded.Any())
        {
            var gwa = graded.Sum(s => s.Grade!.Value * s.Units)
                      / graded.Sum(s => s.Units);
            GwaLabel.Text = gwa.ToString("F2");
            SemGwaLabel.Text = gwa.ToString("F2");
            GwaProgressBar.Progress = Math.Min((double)(gwa / 3.0m), 1.0);
        }
        else
        {
            GwaLabel.Text = "0.00";
            SemGwaLabel.Text = "N/A";
            GwaProgressBar.Progress = 0;
        }
    }

    private async void OnSeeAllClicked(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//SubjectsPage");
    }
    private async void OnAddGradeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SubjectsPage");
    }
    private async void OnPredictorClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//PredictorPage");
    }
}
