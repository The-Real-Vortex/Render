namespace Render.Client.Models;

public class CreatePostModel
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public void Reset()
    {
        Content = string.Empty;
        ImageBytes = Array.Empty<byte>();
    }

    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
}