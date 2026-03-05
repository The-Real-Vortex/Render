using System.ComponentModel.DataAnnotations;

namespace Render.Client.Models;

public class RegisterUserModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "E-Mail is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;
    
    private string? _phoneNumber;

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            _phoneNumber = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
    
    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string Bio { get; set; } = string.Empty;

    public Render.Shared.Models.RegisterUserDto ToDto()
    {
        return new Render.Shared.Models.RegisterUserDto
        {
            Username = this.Username,
            Email = this.Email,
            Password = this.Password,
            PhoneNumber = this.PhoneNumber ?? string.Empty,
            Bio = this.Bio
        };
    }

    public void Reset()
    {
        Username = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        PhoneNumber = null;
        Bio = string.Empty;
    }
}