using Microsoft.EntityFrameworkCore;
using Render.Data;
using Render.Shared;
using Microsoft.Extensions.Caching.Distributed;

namespace Render.Server.Services;

public class LikeService : ILikeService
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ICacheSerializer _serializer;

    public LikeService(AppDbContext context, IDistributedCache cache, ICacheSerializer serializer)
    {
        _context = context;
        _cache = cache;
        _serializer = serializer;
    }

    public async Task LikePostAsync(int postId, int userId)
    {
        var alreadyLiked = await _context.Likes
            .AnyAsync(l => l.PostId == postId && l.UserId == userId);

        if (alreadyLiked)
            return;

        _context.Likes.Add(new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.Now
        });

        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
            post.LikesCount++;

    await _context.SaveChangesAsync();
    // Invalidate cached post DTO
    await _serializer.RemoveAsync(_cache, $"post:{postId}");
    await _serializer.RemoveAsync(_cache, "posts:all");
    }

    public async Task UnlikePostAsync(int postId, int userId)
    {
        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (like == null)
            return;

        _context.Likes.Remove(like);

        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
            post.LikesCount = Math.Max(0, post.LikesCount - 1);

    await _context.SaveChangesAsync();
    // Invalidate cached post DTO
    await _serializer.RemoveAsync(_cache, $"post:{postId}");
    await _serializer.RemoveAsync(_cache, "posts:all");
    }
}
