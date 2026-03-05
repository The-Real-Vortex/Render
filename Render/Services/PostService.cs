using Microsoft.EntityFrameworkCore;
using Render.Data;
using Render.Shared;
using Render.Shared.Models;

namespace Render.Server.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _context;
    public PostService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto, int userId)
    {
        bool hasImage = createPostDto.ImageBytes != null && createPostDto.ImageBytes.Length > 0;
        bool hasContent = !string.IsNullOrWhiteSpace(createPostDto.Content);

        if (!hasImage && !hasContent)
            throw new InvalidOperationException("Content is required when no image is uploaded.");

        if (hasContent && createPostDto.Content!.Length < 10)
            throw new InvalidOperationException("Content must be at least 10 characters long.");

        if (hasContent && createPostDto.Content!.Length > 3500)
            throw new InvalidOperationException("Content must not exceed 3500 characters.");

        var post = new Post
        {
            Content = hasContent ? createPostDto.Content : null,
            Image = createPostDto.ImageBytes,
            CreatedAt = DateTime.Now,
            UserId = userId
        };
        
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return MapToResponseDto(post);
    }

    public static PostResponseDto MapToResponseDto(Post post, bool isLiked = false)
    {
        return new PostResponseDto
        {
            Id = post.Id,
            Content = post.Content,
            Image = post.Image,
            LikesCount = post.LikesCount,
            IsLikedByCurrentUser = isLiked,
            CreatedAt = post.CreatedAt,
            Author = new UserResponseDto
            {
                Id = post.User?.Id ?? 0,
                Username = post.User?.Username ?? "Unknown"
            }
        };
    }

    public async Task<PostResponseDto> GetPostByIdAsync(int postId, int? currentUserId = null)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == postId);
            
        if (post == null)
        {
            throw new InvalidOperationException("Post not found.");
        }

        var isLiked = currentUserId.HasValue &&
            await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == currentUserId.Value);

        return MapToResponseDto(post, isLiked);
    }

    public async Task<List<PostResponseDto>> GetPostsAsync(int take = 10, int skip = 0, int? currentUserId = null)
    {
        var posts = await _context.Posts
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var likedIds = await GetLikedPostIdsAsync(posts.Select(p => p.Id).ToList(), currentUserId);

        return posts.Select(p => MapToResponseDto(p, likedIds.Contains(p.Id))).ToList();
    }

    public async Task<List<int>> GetAllPostIdsAsync()
    {
        return await _context.Posts
            .Select(p => p.Id)
            .ToListAsync();
    }

    public async Task<List<PostResponseDto>> GetPostsByIdsAsync(List<int> ids, int? currentUserId = null)
    {
        var posts = await _context.Posts
            .Include(p => p.User)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        var likedIds = await GetLikedPostIdsAsync(ids, currentUserId);
        var postLookup = posts.ToDictionary(p => p.Id);

        return ids
            .Where(id => postLookup.ContainsKey(id))
            .Select(id => MapToResponseDto(postLookup[id], likedIds.Contains(id)))
            .ToList();
    }

    private async Task<HashSet<int>> GetLikedPostIdsAsync(List<int> postIds, int? userId)
    {
        if (!userId.HasValue || postIds.Count == 0)
            return new HashSet<int>();

        var liked = await _context.Likes
            .Where(l => l.UserId == userId.Value && postIds.Contains(l.PostId))
            .Select(l => l.PostId)
            .ToListAsync();

        return liked.ToHashSet();
    }

    public async Task DeletePostAsync(int postId, int userId)
    {
        var post = await _context.Posts.FindAsync(postId)
            ?? throw new InvalidOperationException("Post not found.");

        if (post.UserId != userId)
            throw new UnauthorizedAccessException("You are not allowed to delete this post.");

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }
}