using GradeTrackerApp.Services;

namespace GradeTrackerApp.Views;

public partial class SubjectsPage : ContentPage
{
    private readonly ApiService _api;
    private List<SubjectResponse> _allSubjects = new();
    private string _selectedSemester = "";
    private List<string> _semesters = new();

    public SubjectsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeSemestersAsync();
        await LoadSubjectsAsync();
    }

    // ── Initialize semesters from both DB and Preferences ──
    private async Task InitializeSemestersAsync()
    {
        // Step 1 — Load from Preferences (fast, offline)
        _semesters = LoadSemestersFromPreferences();

        // Step 2 — Load from server (accurate, persistent)
        var profile = await _api.GetProfileAsync();
        if (profile != null &&
            !string.IsNullOrEmpty(profile.Semesters))
        {
            var serverSemesters = profile.Semesters
                .Split('|')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // Merge server + local semesters
            foreach (var s in serverSemesters)
                if (!_semesters.Contains(s))
                    _semesters.Add(s);
        }

        // Step 3 — Add semesters from existing subjects
        var allSubjects = await _api.GetSubjectsAsync();
        foreach (var s in allSubjects
            .Select(x => x.Semester).Distinct())
        {
            if (!_semesters.Contains(s))
                _semesters.Add(s);
        }

        // Step 4 — Add profile semester if not there
        var profileSemester = Preferences.Get("semester", "");
        if (!string.IsNullOrEmpty(profileSemester) &&
            !_semesters.Contains(profileSemester))
            _semesters.Insert(0, profileSemester);

        // Step 5 — Add default if still empty
        if (!_semesters.Any())
            _semesters.Add(string.IsNullOrEmpty(profileSemester)
                ? "Current Semester"
                : profileSemester);

        // Step 6 — Save merged list to both places
        await SaveSemestersAsync();

        // Step 7 — Set selected semester
        if (string.IsNullOrEmpty(_selectedSemester) ||
            !_semesters.Contains(_selectedSemester))
            _selectedSemester = _semesters.First();

        BuildSemesterTabs();
    }

    // ── Save to Preferences ──
    private void SaveSemestersToPreferences()
    {
        Preferences.Set("saved_semesters",
            string.Join("|", _semesters));
    }

    // ── Load from Preferences ──
    private List<string> LoadSemestersFromPreferences()
    {
        var saved = Preferences.Get("saved_semesters", "");
        if (string.IsNullOrEmpty(saved))
            return new List<string>();

        return saved.Split('|')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
    }

    // ── Save to BOTH Preferences and Database ──
    private async Task SaveSemestersAsync()
    {
        // Save to Preferences
        SaveSemestersToPreferences();

        // Save to Database via API
        await _api.UpdateProfileAsync(
            semesters: string.Join("|", _semesters));
    }

    private void BuildSemesterTabs()
    {
        SemesterTabsLayout.Children.Clear();

        foreach (var sem in _semesters)
        {
            var isSelected = sem == _selectedSemester;

            var tab = new Frame
            {
                BackgroundColor = isSelected
                    ? Color.FromArgb("#1A8C7A")
                    : Color.FromArgb("#E0E0E0"),
                CornerRadius = 20,
                Padding = new Thickness(16, 8),
                BorderColor = Colors.Transparent,
                Content = new Label
                {
                    Text = sem,
                    TextColor = isSelected
                        ? Colors.White : Colors.Gray,
                    FontSize = 12,
                    FontAttributes = isSelected
                        ? FontAttributes.Bold
                        : FontAttributes.None,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            var semCopy = sem;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                _selectedSemester = semCopy;
                BuildSemesterTabs();
                await LoadSubjectsAsync();
            };
            tab.GestureRecognizers.Add(tap);
            SemesterTabsLayout.Children.Add(tab);
        }

        // ── Add (+) Button ──
        var plusBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#1A8C7A"),
            CornerRadius = 20,
            Padding = new Thickness(14, 8),
            BorderColor = Colors.Transparent,
            Content = new Label
            {
                Text = "+",
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            }
        };

        var plusTap = new TapGestureRecognizer();
        plusTap.Tapped += OnAddSemesterClicked;
        plusBtn.GestureRecognizers.Add(plusTap);
        SemesterTabsLayout.Children.Add(plusBtn);
    }

    private async void OnAddSemesterClicked(
        object sender, TappedEventArgs e)
    {
        var newSemester = await DisplayPromptAsync(
            "Add Semester",
            "Enter the semester name:",
            placeholder: "e.g. 2nd Sem 2025-26",
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newSemester)) return;

        newSemester = newSemester.Trim();

        if (_semesters.Contains(newSemester))
        {
            _selectedSemester = newSemester;
            BuildSemesterTabs();
            await LoadSubjectsAsync();
            return;
        }

        // Add semester
        _semesters.Add(newSemester);

        // Save to BOTH places
        await SaveSemestersAsync();

        _selectedSemester = newSemester;
        BuildSemesterTabs();
        await LoadSubjectsAsync();

        await DisplayAlert("✅ Semester Added",
            $"{newSemester} has been added!", "OK");
    }

    private async Task LoadSubjectsAsync()
    {
        _allSubjects = string.IsNullOrEmpty(_selectedSemester)
            ? await _api.GetSubjectsAsync()
            : await _api.GetSubjectsAsync(_selectedSemester);

        SubjectsCollection.ItemsSource = _allSubjects;

        SubjectsEmptyState.IsVisible = !_allSubjects.Any();
        SubjectsCollection.IsVisible = _allSubjects.Any();

        UpdateStats();
    }

    private void UpdateStats()
    {
        var total = _allSubjects.Sum(s => s.Units);
        var done = _allSubjects.Count(s => s.Status == "Done");

        TotalUnitsLabel.Text = total.ToString();
        DoneLabel.Text = $"{done}/{_allSubjects.Count}";

        var graded = _allSubjects
            .Where(s => s.Grade.HasValue).ToList();
        SemGwaLabel.Text = graded.Any()
            ? (graded.Sum(s => s.Grade!.Value * s.Units)
               / graded.Sum(s => s.Units)).ToString("F2")
            : "—";
    }

    private void OnSearchTextChanged(object sender,
        TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? "";
        SubjectsCollection.ItemsSource =
            string.IsNullOrEmpty(query)
                ? _allSubjects
                : _allSubjects.Where(s =>
                    s.Name.ToLower().Contains(query) ||
                    s.Code.ToLower().Contains(query)).ToList();
    }

    private async void OnSubjectTapped(object sender,
        TappedEventArgs e)
    {
        if (e.Parameter is not SubjectResponse subject) return;

        var action = await DisplayActionSheet(
            $"{subject.Code} - {subject.Name}",
            "Cancel", null,
            "Add / Update Grade",
            "Delete Subject");

        if (action == "Add / Update Grade")
            await ShowAddGradeDialog(subject);
        else if (action == "Delete Subject")
            await DeleteSubjectAsync(subject);
    }

    private async Task ShowAddGradeDialog(SubjectResponse subject)
    {
        var gradeStr = await DisplayPromptAsync(
            "Enter Grade",
            $"Enter grade for {subject.Name}\n(1.0 = Best, 3.0 = Pass)",
            placeholder: "e.g. 1.50",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(gradeStr)) return;

        if (!decimal.TryParse(gradeStr, out var grade) ||
            grade < 1.0m || grade > 5.0m)
        {
            await DisplayAlert("Invalid",
                "Please enter a grade between 1.0 and 5.0", "OK");
            return;
        }

        var success = await _api.UpdateGradeAsync(subject.Id, grade);
        if (success)
        {
            await DisplayAlert("Success ✅", "Grade updated!", "OK");
            await LoadSubjectsAsync();
        }
        else
        {
            await DisplayAlert("Error",
                "Failed to update grade.", "OK");
        }
    }

    private async Task DeleteSubjectAsync(SubjectResponse subject)
    {
        var confirm = await DisplayAlert(
            "Delete Subject",
            $"Are you sure you want to delete {subject.Name}?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            var success = await _api.DeleteSubjectAsync(subject.Id);

            if (success)
            {
                _allSubjects = _allSubjects
                    .Where(s => s.Id != subject.Id).ToList();

                SubjectsCollection.ItemsSource = null;
                SubjectsCollection.ItemsSource = _allSubjects;

                SubjectsEmptyState.IsVisible = !_allSubjects.Any();
                SubjectsCollection.IsVisible = _allSubjects.Any();

                UpdateStats();

                await DisplayAlert("Deleted ✅",
                    $"{subject.Name} removed.", "OK");
            }
            else
            {
                await DisplayAlert("Error",
                    "Failed to delete subject.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Something went wrong:\n{ex.Message}", "OK");
        }
    }

    private async void OnAddSubjectClicked(
        object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new AddSubjectPage(_api, _selectedSemester));
    }

    private async void OnAddSubjectButtonClicked(
        object sender, EventArgs e)
    {
        await Navigation.PushAsync(
            new AddSubjectPage(_api, _selectedSemester));
    }
}