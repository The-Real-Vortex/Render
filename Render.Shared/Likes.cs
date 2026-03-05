using System.ComponentModel.DataAnnotations.Schema;

namespace Render.Shared;

public class Like
{
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int PostId { get; set; }
    
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}