namespace GradeTrackerApp.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string College { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string YearLevel { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AcademicGoal { get; set; } = string.Empty;

    public string Semesters { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}