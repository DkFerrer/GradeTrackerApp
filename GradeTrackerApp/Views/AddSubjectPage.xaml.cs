using GradeTrackerApp.Services;

namespace GradeTrackerApp.Views;

public partial class AddSubjectPage : ContentPage
{
    private readonly ApiService _api;
    private readonly string _semester;

    public AddSubjectPage(ApiService api, string semester)
    {
        InitializeComponent();
        _api = api;
        _semester = semester;
        BuildPage();
    }

    private void BuildPage()
    {
        var saveBtn = new Button
        {
            Text = "Add Subject",
            BackgroundColor = Color.FromArgb("#1A8C7A"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 52
        };
        saveBtn.Clicked += OnSaveClicked;

        var cancelBtn = new Button
        {
            Text = "Cancel",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#1A8C7A"),
            BorderColor = Color.FromArgb("#1A8C7A"),
            BorderWidth = 1,
            CornerRadius = 12,
            HeightRequest = 52
        };
        cancelBtn.Clicked += OnCancelClicked;

        _errorLabel = new Label
        {
            TextColor = Colors.Red,
            FontSize = 13,
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center
        };

        _codeEntry = new Entry
        {
            Placeholder = "e.g. 3098",
            BackgroundColor = Colors.White
        };

        _nameEntry = new Entry
        {
            Placeholder = "e.g. Application Development",
            BackgroundColor = Colors.White
        };

        _professorEntry = new Entry
        {
            Placeholder = "e.g. Prof. Cruz",
            BackgroundColor = Colors.White
        };

        _unitsEntry = new Entry
        {
            Placeholder = "e.g. 3",
            Keyboard = Keyboard.Numeric,
            BackgroundColor = Colors.White
        };

        // Show semester as read-only info label
        var semesterInfo = new Frame
        {
            BackgroundColor = Color.FromArgb("#E8F5F3"),
            CornerRadius = 12,
            Padding = new Thickness(16, 10),
            BorderColor = Colors.Transparent,
            Content = new HorizontalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text      = "📅",
                        FontSize  = 16,
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text      = $"Adding to: {_semester}",
                        FontSize  = 13,
                        TextColor = Color.FromArgb("#1A8C7A"),
                        FontAttributes = FontAttributes.Bold,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text           = "Add New Subject",
                        FontSize       = 22,
                        FontAttributes = FontAttributes.Bold,
                        TextColor      = Color.FromArgb("#1A1A1A"),
                        Margin         = new Thickness(0, 10, 0, 0)
                    },
                    semesterInfo,
                    MakeLabel("SUBJECT CODE"),
                    _codeEntry,
                    MakeLabel("SUBJECT NAME"),
                    _nameEntry,
                    MakeLabel("PROFESSOR"),
                    _professorEntry,
                    MakeLabel("UNITS"),
                    _unitsEntry,
                    _errorLabel,
                    saveBtn,
                    cancelBtn
                }
            }
        };

        BackgroundColor = Color.FromArgb("#F5F5F5");
        Title = "Add Subject";
    }

    private Label MakeLabel(string text) => new Label
    {
        Text = text,
        FontSize = 11,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.Gray
    };

    // Field references
    private Entry _codeEntry = new();
    private Entry _nameEntry = new();
    private Entry _professorEntry = new();
    private Entry _unitsEntry = new();
    private Label _errorLabel = new();

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _errorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(_codeEntry.Text) ||
            string.IsNullOrWhiteSpace(_nameEntry.Text) ||
            string.IsNullOrWhiteSpace(_professorEntry.Text) ||
            string.IsNullOrWhiteSpace(_unitsEntry.Text))
        {
            _errorLabel.Text = "Please fill in all fields.";
            _errorLabel.IsVisible = true;
            return;
        }

        if (!int.TryParse(_unitsEntry.Text, out var units))
        {
            _errorLabel.Text = "Units must be a number.";
            _errorLabel.IsVisible = true;
            return;
        }

        try
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            var success = await _api.AddSubjectAsync(
                _codeEntry.Text.Trim(),
                _nameEntry.Text.Trim(),
                _professorEntry.Text.Trim(),
                units,
                _semester);

            if (success)
            {
                await DisplayAlert("Success ✅", "Subject added!", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                _errorLabel.Text = "Failed to add subject.";
                _errorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
            _errorLabel.IsVisible = true;
        }
        finally
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}