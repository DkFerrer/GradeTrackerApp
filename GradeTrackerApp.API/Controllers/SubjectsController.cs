using GradeTrackerApp.API.Data;
using GradeTrackerApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GradeTrackerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                        // ← requires JWT token for all endpoints
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db) => _db = db;

    // Helper: gets the logged-in user's ID from the JWT token
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─────────────────────────────────────────
    // GET /api/subjects
    // GET /api/subjects?semester=1st Sem 2025-26
    // ─────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? semester)
    {
        var query = _db.Subjects
            .Where(s => s.UserId == GetUserId());

        if (!string.IsNullOrEmpty(semester))
            query = query.Where(s => s.Semester == semester);

        return Ok(await query.ToListAsync());
    }

    // ─────────────────────────────────────────
    // GET /api/subjects/5
    // ─────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var subject = await _db.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());

        if (subject == null)
            return NotFound("Subject not found.");

        return Ok(subject);
    }

    // ─────────────────────────────────────────
    // POST /api/subjects
    // ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] SubjectDto dto)
    {
        var subject = new Subject
        {
            UserId = GetUserId(),
            Code = dto.Code,
            Name = dto.Name,
            Professor = dto.Professor,
            Units = dto.Units,
            Semester = dto.Semester,
            Status = "Ongoing"
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = subject.Id }, subject);
    }

    // ─────────────────────────────────────────
    // PATCH /api/subjects/5/grade
    // Body: 1.50
    // ─────────────────────────────────────────
    [HttpPatch("{id}/grade")]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] decimal grade)
    {
        // Validate grade range (1.0 best to 3.0 pass, 5.0 fail)
        if (grade < 1.0m || grade > 5.0m)
            return BadRequest("Grade must be between 1.0 and 5.0.");

        var subject = await _db.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());

        if (subject == null)
            return NotFound("Subject not found.");

        subject.Grade = grade;
        subject.Status = grade <= 3.0m ? "Done" : "Failed";

        await _db.SaveChangesAsync();
        return Ok(subject);
    }

    // ─────────────────────────────────────────
    // DELETE /api/subjects/5
    // ─────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _db.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());

        if (subject == null)
            return NotFound("Subject not found.");

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// DTO — data shape expected from the request body
public record SubjectDto(
    string Code,
    string Name,
    string Professor,
    int Units,
    string Semester
);
