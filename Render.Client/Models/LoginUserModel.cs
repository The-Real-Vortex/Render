using System.ComponentModel.DataAnnotations;

namespace Render.Client.Models;

public class LoginUserModel
{
    [Required(ErrorMessage = "E-Mail is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    public void Reset()
    {
        Email = string.Empty;
        Password = string.Empty;
    }
}