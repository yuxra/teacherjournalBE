using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TeacherJournal.Api.DTOs.Auth;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Teacher> _passwordHasher;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<Teacher>();
    }

    public string HashPassword(Teacher teacher, string password)
    {
        return _passwordHasher.HashPassword(
            teacher,
            password
        );
    }

    public bool VerifyPassword(
        Teacher teacher,
        string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            teacher,
            teacher.PasswordHash,
            password
        );

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public string GenerateToken(Teacher teacher)
    {
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured."
            );
        }

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expirationMinutes =
            _configuration.GetValue<int>(
                "Jwt:ExpirationMinutes"
            );

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, teacher.Id.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                teacher.Id.ToString()
            ),

            new(
                ClaimTypes.Name,
                teacher.Name
            ),

            new(
                ClaimTypes.Email,
                teacher.Email
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                expirationMinutes
            ),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}