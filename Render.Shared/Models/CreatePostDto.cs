namespace Render.Shared.Models;

public class CreatePostDto
{
    public string? Content { get; set; }

    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
}