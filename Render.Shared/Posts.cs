using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Render.Shared;

public class Post
{
    public int Id { get; set; }

    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public string? Content { get; set; }
    
    public byte[]? Image { get; set; }

    public int LikesCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}