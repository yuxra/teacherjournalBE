using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Subject;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // GET ALL SUBJECTS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var subjects = await _db.Subjects
            .AsNoTracking()
            .Where(x => x.TeacherId == teacherId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,

                classes = x.SubjectClasses
                    .OrderBy(sc => sc.Classroom!.Name)
                    .Select(sc => new
                    {
                        id = sc.Classroom!.Id,
                        name = sc.Classroom.Name
                    })
                    .ToList(),

                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(subjects);
    }

    // =====================================================
    // GET SUBJECT BY ID
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var subject = await _db.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TeacherId == teacherId)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,

                classes = x.SubjectClasses
                    .OrderBy(sc => sc.Classroom!.Name)
                    .Select(sc => new
                    {
                        id = sc.Classroom!.Id,
                        name = sc.Classroom.Name
                    })
                    .ToList(),

                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (subject == null)
        {
            return NotFound(new
            {
                message = "Mata pelajaran tidak ditemukan."
            });
        }

        return Ok(subject);
    }

    // =====================================================
    // CREATE SUBJECT
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSubjectRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Nama mata pelajaran wajib diisi."
            });
        }

        var exists = await _db.Subjects
            .AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.Name == name);

        if (exists)
        {
            return Conflict(new
            {
                message =
                    "Mata pelajaran tersebut sudah ada."
            });
        }

        var subject = new Subject
        {
            TeacherId = teacherId.Value,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Subjects.Add(subject);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = subject.Id },
            new
            {
                id = subject.Id,
                name = subject.Name,
                classes = Array.Empty<object>(),
                createdAt = subject.CreatedAt,
                updatedAt = subject.UpdatedAt
            });
    }

    // =====================================================
    // UPDATE SUBJECT
    // =====================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        CreateSubjectRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var subject = await _db.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (subject == null)
        {
            return NotFound(new
            {
                message =
                    "Mata pelajaran tidak ditemukan."
            });
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message =
                    "Nama mata pelajaran wajib diisi."
            });
        }

        var duplicate = await _db.Subjects
            .AnyAsync(x =>
                x.Id != id &&
                x.TeacherId == teacherId &&
                x.Name == name);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Nama mata pelajaran sudah digunakan."
            });
        }

        subject.Name = name;
        subject.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = subject.Id,
            name = subject.Name,
            updatedAt = subject.UpdatedAt
        });
    }

    // =====================================================
    // DELETE SUBJECT
    // =====================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var subject = await _db.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (subject == null)
        {
            return NotFound(new
            {
                message =
                    "Mata pelajaran tidak ditemukan."
            });
        }

        var hasAttendance = await _db.Attendances
            .AnyAsync(x =>
                x.SubjectId == id);

        var hasJournal = await _db.Journals
            .AnyAsync(x =>
                x.SubjectId == id);

        if (hasAttendance || hasJournal)
        {
            return Conflict(new
            {
                message =
                    "Mata pelajaran tidak dapat dihapus karena sudah digunakan."
            });
        }

        _db.Subjects.Remove(subject);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // ADD CLASS TO SUBJECT
    // =====================================================

    [HttpPost("{subjectId:int}/classes/{classroomId:int}")]
    public async Task<IActionResult> AddClass(
        int subjectId,
        int classroomId)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        // Pastikan subject milik teacher
        var subject = await _db.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == subjectId &&
                x.TeacherId == teacherId);

        if (subject == null)
        {
            return NotFound(new
            {
                message =
                    "Mata pelajaran tidak ditemukan."
            });
        }

        // Pastikan classroom milik teacher
        var classroom = await _db.Classrooms
            .FirstOrDefaultAsync(x =>
                x.Id == classroomId &&
                x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message =
                    "Kelas tidak ditemukan."
            });
        }

        // Cek relasi sudah ada
        var exists = await _db.SubjectClasses
            .AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.ClassroomId == classroomId);

        if (exists)
        {
            return Conflict(new
            {
                message =
                    "Mata pelajaran sudah terhubung dengan kelas tersebut."
            });
        }

        var subjectClass = new SubjectClass
        {
            SubjectId = subjectId,
            ClassroomId = classroomId
        };

        _db.SubjectClasses.Add(subjectClass);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = subjectClass.Id,
            subjectId,
            classroomId
        });
    }

    // =====================================================
    // REMOVE CLASS FROM SUBJECT
    // =====================================================

    [HttpDelete("{subjectId:int}/classes/{classroomId:int}")]
    public async Task<IActionResult> RemoveClass(
        int subjectId,
        int classroomId)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var subjectClass = await _db.SubjectClasses
            .Include(x => x.Subject)
            .Include(x => x.Classroom)
            .FirstOrDefaultAsync(x =>
                x.SubjectId == subjectId &&
                x.ClassroomId == classroomId &&
                x.Subject!.TeacherId == teacherId &&
                x.Classroom!.TeacherId == teacherId);

        if (subjectClass == null)
        {
            return NotFound(new
            {
                message =
                    "Relasi mata pelajaran dan kelas tidak ditemukan."
            });
        }

        // Jangan biarkan relasi dihapus kalau
        // sudah pernah digunakan untuk attendance/journal.
        var hasAttendance = await _db.Attendances
            .AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.ClassroomId == classroomId);

        var hasJournal = await _db.Journals
            .AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.ClassroomId == classroomId);

        if (hasAttendance || hasJournal)
        {
            return Conflict(new
            {
                message =
                    "Relasi tidak dapat dihapus karena sudah digunakan."
            });
        }

        _db.SubjectClasses.Remove(subjectClass);

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