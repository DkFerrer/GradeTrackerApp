//using Android.Content.Res;
using GradeTrackerApp.Services;
//using static Android.Provider.ContactsContract;

namespace GradeTrackerApp.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ApiService _api;
    private bool _isEditing = false;

    public ProfilePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileFromServerAsync();
        await LoadStatsAsync();
    }

    // ── Load profile from SERVER ──
    private async Task LoadProfileFromServerAsync()
    {
        var profile = await _api.GetProfileAsync();

        if (profile != null)
        {
            // Save to Preferences for offline use
            Preferences.Set("user_name", profile.FullName);
            Preferences.Set("user_program", profile.Program);
            Preferences.Set("college", profile.College);
            Preferences.Set("semester", profile.Semester);
            Preferences.Set("user_student_id", profile.StudentId);
            Preferences.Set("year_level", profile.YearLevel);
            Preferences.Set("academic_goal", profile.AcademicGoal);
        }

        LoadProfileInfo();
    }

    private void LoadProfileInfo()
    {
        var name = Preferences.Get("user_name", "Student");

        FullNameLabel.Text = name;
        var initials = string.Join("",
            name.Split(' ').Take(2)
                .Select(w => w[0].ToString().ToUpper()));
        AvatarLabel.Text = initials;

        ProgramEntry.Text = Preferences.Get("user_program", "");
        CollegeEntry.Text = Preferences.Get("college", "");
        SemesterEntry.Text = Preferences.Get("semester", "");
        StudentIdEntry.Text = Preferences.Get("user_student_id", "");
        YearLevelEntry.Text = Preferences.Get("year_level", "");

        UpdateSubtitle();

        var goal = Preferences.Get("academic_goal", "Magna Cum Laude");
        GoalTitleLabel.Text = goal;
        GoalDescLabel.Text =
            $"Target GWA: {GetTargetGwaText(goal)} by Graduation";

        // Start in view mode
        SetEditMode(false);
    }

    private void UpdateSubtitle()
    {
        var studentId = Preferences.Get("user_student_id", "");
        var yearLevel = Preferences.Get("year_level", "");

        if (!string.IsNullOrEmpty(studentId) &&
            !string.IsNullOrEmpty(yearLevel))
            StudentIdYearLabel.Text = $"{studentId} · {yearLevel}";
        else if (!string.IsNullOrEmpty(studentId))
            StudentIdYearLabel.Text = studentId;
        else if (!string.IsNullOrEmpty(yearLevel))
            StudentIdYearLabel.Text = yearLevel;
        else
            StudentIdYearLabel.Text = "Tap Edit to update profile";
    }

    // ── Toggle Edit / Save Mode ──
    private void SetEditMode(bool editing)
    {
        _isEditing = editing;

        // Toggle fields read-only
        ProgramEntry.IsReadOnly = !editing;
        CollegeEntry.IsReadOnly = !editing;
        SemesterEntry.IsReadOnly = !editing;
        StudentIdEntry.IsReadOnly = !editing;
        YearLevelEntry.IsReadOnly = !editing;

        // Change field background to show editable state
        var bgColor = editing
            ? Color.FromArgb("#F0FFF8")
            : Colors.Transparent;

        ProgramEntry.BackgroundColor = bgColor;
        CollegeEntry.BackgroundColor = bgColor;
        SemesterEntry.BackgroundColor = bgColor;
        StudentIdEntry.BackgroundColor = bgColor;
        YearLevelEntry.BackgroundColor = bgColor;

        // Toggle button text
        SaveEditButton.Text = editing
            ? "Save Profile"
            : "Edit Profile";

        SaveEditButton.BackgroundColor = editing
            ? Color.FromArgb("#1A8C7A")
            : Color.FromArgb("#F47B20");
    }

    // ── Save / Edit Button Clicked ──
    private async void OnSaveEditClicked(object sender, EventArgs e)
    {
        if (!_isEditing)
        {
            // Switch to edit mode
            SetEditMode(true);
            return;
        }

        // Save to server
        var profile = await _api.UpdateProfileAsync(
            college: CollegeEntry.Text?.Trim(),
            studentId: StudentIdEntry.Text?.Trim(),
            yearLevel: YearLevelEntry.Text?.Trim(),
            semester: SemesterEntry.Text?.Trim(),
            program: ProgramEntry.Text?.Trim(),
            academicGoal: Preferences.Get("academic_goal",
                              "Magna Cum Laude"));

        if (profile != null)
        {
            // Update Preferences
            Preferences.Set("user_program",
                ProgramEntry.Text?.Trim() ?? "");
            Preferences.Set("college",
                CollegeEntry.Text?.Trim() ?? "");
            Preferences.Set("semester",
                SemesterEntry.Text?.Trim() ?? "");
            Preferences.Set("user_student_id",
                StudentIdEntry.Text?.Trim() ?? "");
            Preferences.Set("year_level",
                YearLevelEntry.Text?.Trim() ?? "");

            UpdateSubtitle();
            SetEditMode(false);
            await DisplayAlert("Saved ✅",
                "Profile updated successfully!", "OK");
        }
        else
        {
            await DisplayAlert("Error",
                "Failed to save profile. Please try again.", "OK");
        }
    }

    private void OnProfileFieldCompleted(object sender, EventArgs e)
    {
        // Auto-save to preferences as user types
        Preferences.Set("user_program",
            ProgramEntry.Text?.Trim() ?? "");
        Preferences.Set("college",
            CollegeEntry.Text?.Trim() ?? "");
        Preferences.Set("semester",
            SemesterEntry.Text?.Trim() ?? "");
        Preferences.Set("user_student_id",
            StudentIdEntry.Text?.Trim() ?? "");
        Preferences.Set("year_level",
            YearLevelEntry.Text?.Trim() ?? "");
    }

    private string GetTargetGwaText(string goal) => goal switch
    {
        "Summa Cum Laude" => "≤ 1.20",
        "Magna Cum Laude" => "≤ 1.30",
        "Cum Laude" => "≤ 1.40",
        "Dean's Lister" => "≤ 1.50",
        _ => "≤ 1.30"
    };

    private decimal GetTargetGwaDecimal(string goal) => goal switch
    {
        "Summa Cum Laude" => 1.20m,
        "Magna Cum Laude" => 1.30m,
        "Cum Laude" => 1.40m,
        "Dean's Lister" => 1.50m,
        _ => 1.30m
    };

    private async Task LoadStatsAsync()
    {
        var subjects = await _api.GetSubjectsAsync();
        var graded = subjects.Where(s => s.Grade.HasValue).ToList();
        var totalUnits = subjects.Sum(s => s.Units);

        TotalUnitsLabel.Text = totalUnits.ToString();

        if (graded.Any())
        {
            var cumGwa = graded.Sum(s => s.Grade!.Value * s.Units)
                         / graded.Sum(s => s.Units);

            CumGwaLabel.Text = cumGwa.ToString("F2");
            BestGwaLabel.Text = cumGwa.ToString("F2");
            SemestersLabel.Text = "1";

            var goal = Preferences.Get("academic_goal",
                                "Magna Cum Laude");
            var targetGwa = GetTargetGwaDecimal(goal);
            var progress = cumGwa <= targetGwa ? 1.0
                : Math.Max(0,
                    1.0 - (double)((cumGwa - targetGwa) / targetGwa));

            GoalProgressBar.Progress = progress;
            GoalProgressLabel.Text =
                $"{(int)(progress * 100)}% towards goal";
        }
        else
        {
            CumGwaLabel.Text = "—";
            TotalUnitsLabel.Text = "0";
            SemestersLabel.Text = "0";
            BestGwaLabel.Text = "—";
            GoalProgressBar.Progress = 0;
            GoalProgressLabel.Text = "0% towards goal";
        }
    }

    private async void OnEditProfileClicked(
        object sender, TappedEventArgs e)
    {
        SetEditMode(true);
    }

    private async void OnEditGoalClicked(
        object sender, TappedEventArgs e)
    {
        var goal = await DisplayActionSheet(
            "Select Academic Goal", "Cancel", null,
            "Summa Cum Laude (GWA ≤ 1.20)",
            "Magna Cum Laude (GWA ≤ 1.30)",
            "Cum Laude (GWA ≤ 1.40)",
            "Dean's Lister (GWA ≤ 1.50)");

        if (goal == null || goal == "Cancel") return;

        var goalName = goal.Split('(')[0].Trim();
        Preferences.Set("academic_goal", goalName);
        GoalTitleLabel.Text = goalName;
        GoalDescLabel.Text =
            $"Target GWA: {GetTargetGwaText(goalName)} by Graduation";

        await LoadStatsAsync();
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current!.UserAppTheme = e.Value
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    private async void OnAcademicSettingsTapped(
        object sender, TappedEventArgs e)
    {
        await DisplayAlert("Academic Settings",
            "Coming soon!", "OK");
    }

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Sign Out",
            "Are you sure you want to sign out?",
            "Sign Out", "Cancel");

        if (!confirm) return;

        Preferences.Remove("jwt_token");
        Preferences.Remove("user_name");
        Preferences.Remove("user_email");
        Preferences.Remove("user_program");
        Preferences.Remove("college");
        Preferences.Remove("semester");
        Preferences.Remove("user_student_id");
        Preferences.Remove("year_level");
        Preferences.Remove("academic_goal");

        Application.Current!.MainPage =
            new NavigationPage(new LoginPage(
                Handler!.MauiContext!.Services
                    .GetService<ApiService>()!));
    }
}
