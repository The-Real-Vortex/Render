using Render.Shared.Models;

public class PostResponseDto
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public byte[]? Image { get; set; }
    public int LikesCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto Author { get; set; } = new UserResponseDto();
}