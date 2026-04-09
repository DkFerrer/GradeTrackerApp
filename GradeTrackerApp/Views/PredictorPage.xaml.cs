using GradeTrackerApp.Services;
using Microsoft.Maui.Layouts;

namespace GradeTrackerApp.Views;

public partial class PredictorPage : ContentPage
{
    private readonly ApiService _api;
    private List<SubjectResponse> _subjects = new();
    private string _selectedHonor = "CumLaude";
    private decimal _targetGwa = 1.40m;

    // Honor thresholds
    private readonly Dictionary<string, (decimal Gwa, string Label, string Color)> _honors = new()
    {
        { "Summa",    (1.20m, "Summa Cum Laude",  "#9C27B0") },
        { "Magna",    (1.30m, "Magna Cum Laude",   "#2196F3") },
        { "CumLaude", (1.40m, "Cum Laude",         "#4CAF50") },
        { "Deans",    (1.50m, "Dean's Lister",     "#F47B20") }
    };

    // What-if simulated grades per subject id
    private readonly Dictionary<int, decimal> _simulatedGrades = new();

    private readonly List<decimal> _gradeOptions = new()
    {
        1.00m, 1.25m, 1.50m, 1.75m,
        2.00m, 2.25m, 2.50m, 2.75m, 3.00m
    };

    public PredictorPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _subjects = await _api.GetSubjectsAsync();

        if (!_subjects.Any())
        {
            // Show empty state
            await DisplayAlert(
                "No Subjects Yet",
                "Please add your subjects first before using the predictor.",
                "OK");
            return;
        }

        UpdateHonorSelection();
        UpdateGwaComparison();
        UpdateRequiredGrades();
        BuildSimulator();
        UpdateTips();
    }

    private async void OnGoToSubjectsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SubjectsPage");
    }

    // ── Honor selection ──
    private void OnHonorSelected(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string honor) return;
        _selectedHonor = honor;
        _targetGwa = _honors[honor].Gwa;
        UpdateHonorSelection();
        UpdateGwaComparison();
        UpdateRequiredGrades();
        UpdateTips();
    }

    private void UpdateHonorSelection()
    {
        // Reset all borders
        SummaFrame.BorderColor = Colors.Transparent;
        MagnaFrame.BorderColor = Colors.Transparent;
        CumLaudeFrame.BorderColor = Colors.Transparent;
        DeansFrame.BorderColor = Colors.Transparent;

        // Highlight selected
        var selected = _selectedHonor switch
        {
            "Summa" => SummaFrame,
            "Magna" => MagnaFrame,
            "CumLaude" => CumLaudeFrame,
            "Deans" => DeansFrame,
            _ => CumLaudeFrame
        };

        var color = Color.FromArgb(_honors[_selectedHonor].Color);
        selected.BorderColor = color;
    }

    // ── GWA Comparison ──
    private void UpdateGwaComparison()
    {
        var graded = _subjects.Where(s => s.Grade.HasValue).ToList();
        var currentGwa = graded.Any()
            ? graded.Sum(s => s.Grade!.Value * s.Units) / graded.Sum(s => s.Units)
            : 0m;

        CurrentGwaLabel.Text = currentGwa.ToString("F2");
        TargetGwaLabel.Text = $"≤{_targetGwa:F2}";
        TargetHonorLabel.Text = _honors[_selectedHonor].Label;

        var color = Color.FromArgb(_honors[_selectedHonor].Color);
        TargetGwaLabel.TextColor = color;
        TargetHonorLabel.TextColor = color;
    }

    // ── Required Grades ──
    private void UpdateRequiredGrades()
    {
        var ongoing = _subjects.Where(s => s.Status == "Ongoing").ToList();
        var graded = _subjects.Where(s => s.Grade.HasValue).ToList();

        decimal gradedWeighted = graded.Any()
            ? graded.Sum(s => s.Grade!.Value * s.Units)
            : 0m;
        int gradedUnits = graded.Sum(s => s.Units);
        int ongoingUnits = ongoing.Sum(s => s.Units);
        int totalUnits = gradedUnits + ongoingUnits;

        // Required average for ongoing: solve for x
        // (gradedWeighted + x * ongoingUnits) / totalUnits = targetGwa
        decimal requiredAvg = totalUnits > 0 && ongoingUnits > 0
            ? (_targetGwa * totalUnits - gradedWeighted) / ongoingUnits
            : _targetGwa;

        var items = ongoing.Select(s => new RequiredGradeItem
        {
            Code = s.Code,
            Name = s.Name,
            RequiredGrade = Math.Max(1.0m, Math.Round(requiredAvg * 4) / 4)
        }).ToList();

        RequiredGradesCollection.ItemsSource = items;
    }

    // ── What-If Simulator ──
    private void BuildSimulator()
    {
        var ongoing = _subjects.Where(s => s.Status == "Ongoing").ToList();
        var items = new List<SimulatorItem>();

        foreach (var subject in ongoing)
        {
            var layout = new FlexLayout
            {
                Wrap = FlexWrap.Wrap,
                Direction = FlexDirection.Row
            };

            var subjectCopy = subject;

            foreach (var grade in _gradeOptions)
            {
                var gradeCopy = grade;
                var isSelected = _simulatedGrades.ContainsKey(subjectCopy.Id)
                                    && _simulatedGrades[subjectCopy.Id] == grade;
                var btn = new Frame
                {
                    BackgroundColor = isSelected
                        ? Color.FromArgb("#1A8C7A")
                        : Color.FromArgb("#F0F0F0"),
                    CornerRadius = 8,
                    Padding = new Thickness(10, 6),
                    Margin = new Thickness(4),
                    BorderColor = Colors.Transparent
                };

                var lbl = new Label
                {
                    Text = grade.ToString("F2"),
                    TextColor = isSelected ? Colors.White : Colors.Black,
                    FontSize = 12
                };

                btn.Content = lbl;

                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) =>
                {
                    _simulatedGrades[subjectCopy.Id] = gradeCopy;
                    BuildSimulator();
                    UpdateSimulatedGwa();
                };
                btn.GestureRecognizers.Add(tap);
                layout.Children.Add(btn);
            }

            items.Add(new SimulatorItem
            {
                SubjectLabel = $"{subject.Code} {subject.Name}",
                GradeLayout = layout
            });
        }

        SimulatorCollection.ItemsSource = items;
    }

    // ── Simulated GWA ──
    private void UpdateSimulatedGwa()
    {
        var allSubjects = _subjects.Select(s =>
        {
            var grade = _simulatedGrades.ContainsKey(s.Id)
                ? _simulatedGrades[s.Id]
                : s.Grade ?? 0m;
            return (grade, s.Units);
        }).ToList();

        if (!allSubjects.Any(x => x.grade > 0)) return;

        var simGwa = allSubjects.Where(x => x.grade > 0)
            .Sum(x => x.grade * x.Units)
            / allSubjects.Where(x => x.grade > 0)
            .Sum(x => x.Units);

        CurrentGwaLabel.Text = $"{simGwa:F2}*";
    }

    // ── Personalized Tips ──
    private void UpdateTips()
    {
        TipsLayout.Children.Clear();

        var tips = GenerateTips();
        for (int i = 0; i < tips.Count; i++)
        {
            var tip = new HorizontalStackLayout { Spacing = 8 };
            tip.Children.Add(new Label
            {
                Text = $"{i + 1}",
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13
            });
            tip.Children.Add(new Label
            {
                Text = tips[i],
                TextColor = Colors.White,
                FontSize = 13
            });
            TipsLayout.Children.Add(tip);
        }
    }

    private List<string> GenerateTips()
    {
        var tips = new List<string>();
        var ongoing = _subjects.Where(s => s.Status == "Ongoing").ToList();
        var graded = _subjects.Where(s => s.Grade.HasValue).ToList();

        if (ongoing.Count == 1)
            tips.Add($"Focus on {ongoing[0].Code} — it's your only ongoing subject.");

        var currentGwa = graded.Any()
            ? graded.Sum(s => s.Grade!.Value * s.Units) / graded.Sum(s => s.Units)
            : 0m;

        if (currentGwa > 0 && currentGwa <= _targetGwa)
            tips.Add($"You're on track for {_honors[_selectedHonor].Label}! Keep it up.");
        else if (currentGwa > _targetGwa)
            tips.Add($"You need to improve your grades to reach {_honors[_selectedHonor].Label}.");

        if (graded.Any())
        {
            var lowest = graded.OrderByDescending(s => s.Grade).First();
            tips.Add($"Review {lowest.Code} — it's your lowest performing subject.");
        }

        if (!tips.Any())
            tips.Add("Add your grades to get personalized tips!");

        return tips;
    }
}

// ── Helper Models ──
public class RequiredGradeItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal RequiredGrade { get; set; }
}

public class SimulatorItem
{
    public string SubjectLabel { get; set; } = string.Empty;
    public FlexLayout GradeLayout { get; set; } = new();
}
