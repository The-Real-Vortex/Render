using System.ComponentModel.DataAnnotations;

namespace Render.Shared.Models;

public class RegisterUserDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string Bio { get; set; } = string.Empty;
}