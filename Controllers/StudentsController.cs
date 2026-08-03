using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Student;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudentsController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // GET STUDENTS BY CLASSROOM
    // =====================================================

    [HttpGet("/api/classrooms/{classroomId:int}/students")]
    public async Task<IActionResult> GetByClassroom(
        int classroomId)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroomExists = await _db.Classrooms
            .AnyAsync(x =>
                x.Id == classroomId &&
                x.TeacherId == teacherId);

        if (!classroomExists)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        var students = await _db.Students
            .AsNoTracking()
            .Where(x =>
                x.ClassroomId == classroomId &&
                x.IsActive)
            .OrderBy(x => x.AttendanceNumber)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                nisn = x.NISN,
                attendanceNumber = x.AttendanceNumber,
                isActive = x.IsActive,
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(students);
    }

    // =====================================================
    // CREATE STUDENT
    // =====================================================

    [HttpPost("/api/classrooms/{classroomId:int}/students")]
    public async Task<IActionResult> Create(
        int classroomId,
        CreateStudentRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroomExists = await _db.Classrooms
            .AnyAsync(x =>
                x.Id == classroomId &&
                x.TeacherId == teacherId);

        if (!classroomExists)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        var name = request.Name.Trim();
        var nisn = request.NISN.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Nama siswa wajib diisi."
            });
        }

        if (string.IsNullOrWhiteSpace(nisn))
        {
            return BadRequest(new
            {
                message = "NISN wajib diisi."
            });
        }

        var attendanceNumberExists =
            await _db.Students.AnyAsync(x =>
                x.ClassroomId == classroomId &&
                x.AttendanceNumber ==
                    request.AttendanceNumber &&
                x.IsActive);

        if (attendanceNumberExists)
        {
            return Conflict(new
            {
                message =
                    "Nomor absen tersebut sudah digunakan."
            });
        }

        var nisnExists =
            await _db.Students.AnyAsync(x =>
                x.NISN == nisn &&
                x.IsActive);

        if (nisnExists)
        {
            return Conflict(new
            {
                message =
                    "NISN tersebut sudah digunakan."
            });
        }

        var student = new Student
        {
            ClassroomId = classroomId,
            Name = name,
            NISN = nisn,
            AttendanceNumber =
                request.AttendanceNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Students.Add(student);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = student.Id,
            name = student.Name,
            nisn = student.NISN,
            attendanceNumber =
                student.AttendanceNumber,
            isActive = student.IsActive,
            createdAt = student.CreatedAt,
            updatedAt = student.UpdatedAt
        });
    }

    // =====================================================
    // UPDATE STUDENT
    // =====================================================

    [HttpPut("{studentId:int}")]
    public async Task<IActionResult> Update(
        int studentId,
        UpdateStudentRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var student = await _db.Students
            .Include(x => x.Classroom)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.Classroom != null &&
                x.Classroom.TeacherId == teacherId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Siswa tidak ditemukan."
            });
        }

        var name = request.Name.Trim();
        var nisn = request.NISN.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Nama siswa wajib diisi."
            });
        }

        if (string.IsNullOrWhiteSpace(nisn))
        {
            return BadRequest(new
            {
                message = "NISN wajib diisi."
            });
        }

        var attendanceNumberExists =
            await _db.Students.AnyAsync(x =>
                x.Id != studentId &&
                x.ClassroomId ==
                    student.ClassroomId &&
                x.AttendanceNumber ==
                    request.AttendanceNumber &&
                x.IsActive);

        if (attendanceNumberExists)
        {
            return Conflict(new
            {
                message =
                    "Nomor absen tersebut sudah digunakan."
            });
        }

        var nisnExists =
            await _db.Students.AnyAsync(x =>
                x.Id != studentId &&
                x.NISN == nisn &&
                x.IsActive);

        if (nisnExists)
        {
            return Conflict(new
            {
                message =
                    "NISN tersebut sudah digunakan."
            });
        }

        student.Name = name;
        student.NISN = nisn;
        student.AttendanceNumber =
            request.AttendanceNumber;

        student.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = student.Id,
            name = student.Name,
            nisn = student.NISN,
            attendanceNumber =
                student.AttendanceNumber,
            updatedAt = student.UpdatedAt
        });
    }

    // =====================================================
    // DELETE / DEACTIVATE STUDENT
    // =====================================================

    [HttpDelete("{studentId:int}")]
    public async Task<IActionResult> Delete(
        int studentId)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var student = await _db.Students
            .Include(x => x.Classroom)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.Classroom != null &&
                x.Classroom.TeacherId == teacherId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Siswa tidak ditemukan."
            });
        }

        // Soft delete
        student.IsActive = false;
        student.UpdatedAt = DateTime.UtcNow;

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