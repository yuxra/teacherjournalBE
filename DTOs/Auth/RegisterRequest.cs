using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage =
            "Password harus memiliki minimal 8 karakter, " +
            "mengandung huruf besar, huruf kecil, dan angka."
    )]
    public string Password { get; set; } = string.Empty;
}