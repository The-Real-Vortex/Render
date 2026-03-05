using Microsoft.EntityFrameworkCore;
using Render.Data;
using Render.Shared;

namespace Render.Server.Services;

public class LikeService : ILikeService
{
    private readonly AppDbContext _context;

    public LikeService(AppDbContext context)
    {
        _context = context;
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
    }
}
