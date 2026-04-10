using GradeTrackerApp.API.Data;
using GradeTrackerApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GradeTrackerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Program = dto.Program,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Account created successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Debug: check if user exists
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            return Unauthorized($"No user found with email: {dto.Email}");

        // Debug: check password
        var passwordValid = BCrypt.Net.BCrypt.Verify(
            dto.Password, user.PasswordHash);

        if (!passwordValid)
            return Unauthorized($"Password incorrect for: {dto.Email}");

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            user.FullName,
            user.Email,
            user.Program
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    // GET /api/auth/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(
            ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.FullName,
            user.Email,
            user.Program,
            user.College,
            user.StudentId,
            user.YearLevel,
            user.Semester,
            user.AcademicGoal,
            user.Semesters        // ← add this
        });
    }

    // PUT /api/auth/profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(
            ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.College = dto.College ?? user.College;
        user.StudentId = dto.StudentId ?? user.StudentId;
        user.YearLevel = dto.YearLevel ?? user.YearLevel;
        user.Semester = dto.Semester ?? user.Semester;
        user.Program = dto.Program ?? user.Program;
        user.AcademicGoal = dto.AcademicGoal ?? user.AcademicGoal;
        user.Semesters = dto.Semesters ?? user.Semesters; // ← add

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.FullName,
            user.Email,
            user.Program,
            user.College,
            user.StudentId,
            user.YearLevel,
            user.Semester,
            user.AcademicGoal,
            user.Semesters        // ← add this
        });
    }

    public record UpdateProfileDto(
        string? College,
        string? StudentId,
        string? YearLevel,
        string? Semester,
        string? Program,
        string? AcademicGoal,
        string? Semesters         // ← add this
    );

    public record RegisterDto(string FullName, string Email, string Program, string Password);
    public record LoginDto(string Email, string Password);
}


