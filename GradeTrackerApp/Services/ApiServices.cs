using GradeTrackerApp.Views;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GradeTrackerApp.Services;

public class ApiService
{
    private readonly HttpClient _http;

#if ANDROID
    private const string BaseUrl = "http://10.14.1.21:7131/api";
#else
    private const string BaseUrl = "http://localhost:7131/api";
#endif

    public ApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _http = new HttpClient(handler);
    }

    public void SetToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/login",
                new { email, password });

            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LoginResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RegisterAsync(string fullName, string email,
        string program, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/register",
                new { fullName, email, program, password });
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<SubjectResponse>> GetSubjectsAsync(
        string? semester = null)
    {
        try
        {
            var url = $"{BaseUrl}/subjects";
            if (semester != null)
                url += $"?semester={Uri.EscapeDataString(semester)}";
            return await _http.GetFromJsonAsync<List<SubjectResponse>>(url)
                   ?? new List<SubjectResponse>();
        }
        catch
        {
            return new List<SubjectResponse>();
        }
    }

    public async Task<bool> AddSubjectAsync(string code, string name,
        string professor, int units, string semester)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"{BaseUrl}/subjects",
                new { code, name, professor, units, semester });

            var content = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"AddSubject response: {res.StatusCode} - {content}");

            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AddSubject error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        try
        {
            var res = await _http.DeleteAsync(
                $"{BaseUrl}/subjects/{subjectId}");
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteSubject error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateGradeAsync(int subjectId, decimal grade)
    {
        try
        {
            var res = await _http.PatchAsJsonAsync(
                $"{BaseUrl}/subjects/{subjectId}/grade", grade);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProfileResponse?> GetProfileAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<ProfileResponse>(
                $"{BaseUrl}/auth/profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetProfile error: {ex.Message}");
            return null;
        }
    }

    public async Task<ProfileResponse?> UpdateProfileAsync(
        string? college = null,
        string? studentId = null,
        string? yearLevel = null,
        string? semester = null,
        string? program = null,
        string? academicGoal = null,
        string? semesters = null)
    {
        try
        {
            var res = await _http.PutAsJsonAsync(
                $"{BaseUrl}/auth/profile",
                new
                {
                    college,
                    studentId,
                    yearLevel,
                    semester,
                    program,
                    academicGoal,
                    semesters
                });

            if (!res.IsSuccessStatusCode) return null;
            return await res.Content
                .ReadFromJsonAsync<ProfileResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateProfile error: {ex.Message}");
            return null;
        }
    }

}

// Response Models
public record LoginResponse(
    string Token,
    string FullName,
    string Email,
    string Program
);

public record SubjectResponse(
    int Id,
    string Code,
    string Name,
    string Professor,
    int Units,
    string Semester,
    decimal? Grade,
    string Status
);
public record ProfileResponse(
    string FullName,
    string Email,
    string Program,
    string College,
    string StudentId,
    string YearLevel,
    string Semester,
    string AcademicGoal,
    string Semesters
);
