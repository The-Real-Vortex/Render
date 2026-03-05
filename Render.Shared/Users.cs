using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Render.Shared;

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string Bio { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    
    [JsonIgnore]
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}