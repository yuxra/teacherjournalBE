using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Classroom;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassroomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClassroomsController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // GET ALL CLASSROOMS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classrooms = await _db.Classrooms
            .AsNoTracking()
            .Where(x => x.TeacherId == teacherId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                studentCount = x.Students.Count(s => s.IsActive),
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(classrooms);
    }

    // =====================================================
    // GET CLASSROOM DETAIL
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroom = await _db.Classrooms
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TeacherId == teacherId)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                studentCount = x.Students.Count(s => s.IsActive),
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (classroom == null)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        return Ok(classroom);
    }

    // =====================================================
    // CREATE CLASSROOM
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateClassroomRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Nama kelas wajib diisi."
            });
        }

        var exists = await _db.Classrooms
            .AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message = "Kelas dengan nama tersebut sudah ada."
            });
        }

        var classroom = new Classroom
        {
            TeacherId = teacherId.Value,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Classrooms.Add(classroom);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = classroom.Id },
            new
            {
                id = classroom.Id,
                name = classroom.Name,
                studentCount = 0,
                createdAt = classroom.CreatedAt,
                updatedAt = classroom.UpdatedAt
            });
    }

    // =====================================================
    // UPDATE CLASSROOM
    // =====================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        CreateClassroomRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroom = await _db.Classrooms
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Nama kelas wajib diisi."
            });
        }

        var duplicate = await _db.Classrooms
            .AnyAsync(x =>
                x.Id != id &&
                x.TeacherId == teacherId &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Nama kelas sudah digunakan."
            });
        }

        classroom.Name = name;
        classroom.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = classroom.Id,
            name = classroom.Name,
            updatedAt = classroom.UpdatedAt
        });
    }

    // =====================================================
    // DELETE CLASSROOM
    // =====================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroom = await _db.Classrooms
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        var hasStudents = await _db.Students
            .AnyAsync(x =>
                x.ClassroomId == id);

        var hasAttendances = await _db.Attendances
            .AnyAsync(x =>
                x.ClassroomId == id);

        var hasJournals = await _db.Journals
            .AnyAsync(x =>
                x.ClassroomId == id);

        if (hasStudents ||
            hasAttendances ||
            hasJournals)
        {
            return Conflict(new
            {
                message =
                    "Kelas tidak dapat dihapus karena sudah memiliki data terkait."
            });
        }

        _db.Classrooms.Remove(classroom);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // HELPER
    // =====================================================

    private int? GetTeacherId()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
            claim,
            out var teacherId))
        {
            return teacherId;
        }

        return null;
    }
}