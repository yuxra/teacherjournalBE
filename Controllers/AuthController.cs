using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Auth;
using TeacherJournal.Api.Models;
using TeacherJournal.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _authService;

    public AuthController(
        AppDbContext db,
        AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    // =====================================================
    // REGISTER
    // =====================================================

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingTeacher =
            await _db.Teachers
                .FirstOrDefaultAsync(x =>
                    x.Email == email);

        if (existingTeacher != null)
        {
            return Conflict(new
            {
                message = "Email sudah terdaftar."
            });
        }

        var teacher = new Teacher
        {
            Name = request.Name.Trim(),

            Email = email,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        teacher.PasswordHash =
            _authService.HashPassword(
                teacher,
                request.Password
            );

        _db.Teachers.Add(teacher);

        await _db.SaveChangesAsync();

        var token =
            _authService.GenerateToken(teacher);

        return Ok(new AuthResponse
        {
            Id = teacher.Id,

            Name = teacher.Name,

            Email = teacher.Email,

            Token = token
        });
    }

    // =====================================================
    // LOGIN
    // =====================================================

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
    {
        var email =
            request.Email.Trim().ToLowerInvariant();

        var teacher =
            await _db.Teachers
                .FirstOrDefaultAsync(x =>
                    x.Email == email);

        if (teacher == null)
        {
            return Unauthorized(new
            {
                message = "Email atau password salah."
            });
        }

        var passwordValid =
            _authService.VerifyPassword(
                teacher,
                request.Password
            );

        if (!passwordValid)
        {
            return Unauthorized(new
            {
                message = "Email atau password salah."
            });
        }

        var token =
            _authService.GenerateToken(teacher);

        return Ok(new AuthResponse
        {
            Id = teacher.Id,

            Name = teacher.Name,

            Email = teacher.Email,

            Token = token
        });
    }

    // =====================================================
    // GET CURRENT LOGGED-IN TEACHER
    // =====================================================

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var teacherIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(teacherIdClaim))
        {
            return Unauthorized(new
            {
                message = "Token tidak memiliki identitas user."
            });
        }

        if (!int.TryParse(
            teacherIdClaim,
            out var teacherId))
        {
            return Unauthorized(new
            {
                message = "Teacher ID tidak valid."
            });
        }

        var teacher =
            await _db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == teacherId);

        if (teacher == null)
        {
            return NotFound(new
            {
                message = "Teacher tidak ditemukan."
            });
        }

        return Ok(new
        {
            id = teacher.Id,
            name = teacher.Name,
            email = teacher.Email
        });
    }
}