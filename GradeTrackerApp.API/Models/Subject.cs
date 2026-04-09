namespace GradeTrackerApp.API.Models;

public class Subject
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public int Units { get; set; }
    public string Semester { get; set; } = string.Empty; // e.g. "1st Sem 2025-26"
    public decimal? Grade { get; set; }       // null = Ongoing
    public string Status { get; set; } = "Ongoing"; // Ongoing / Done

    public User User { get; set; } = null!;
}