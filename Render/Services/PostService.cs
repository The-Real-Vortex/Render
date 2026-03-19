using Microsoft.EntityFrameworkCore;
using Render.Data;
using Render.Shared;
using Render.Shared.Models;

namespace Render.Server.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _context;
    private readonly IShardedCache _cache;

    public PostService(AppDbContext context, IShardedCache cache)
    {
        _context = context;
        _cache = cache;
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

        // Invalidate caches related to posts
        await _cache.RemoveAsync($"post:{post.Id}");
        await _cache.RemoveAsync("posts:all");
        await _cache.RemoveAsync("posts:ids");

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
        var cacheKey = $"post:{postId}";
        var cached = await _cache.GetAsync<PostResponseDto>(cacheKey);
        if (cached != null)
        {
            if (currentUserId.HasValue)
            {
                var isLiked = await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == currentUserId.Value);
                cached.IsLikedByCurrentUser = isLiked;
            }
            return cached;
        }

        var post = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            throw new InvalidOperationException("Post not found.");

        var isLikedDb = currentUserId.HasValue &&
            await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == currentUserId.Value);

        var dto = MapToResponseDto(post, isLikedDb);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30));
        return dto;
    }

    public async Task<List<PostResponseDto>> GetPostsAsync(int take = 10, int skip = 0, int? currentUserId = null)
    {
        var cacheKey = "posts:all";
        var cached = await _cache.GetAsync<List<PostResponseDto>>(cacheKey);
        if (cached != null)
        {
            if (currentUserId.HasValue)
            {
                var likedIds = await GetLikedPostIdsAsync(cached.Select(p => p.Id).ToList(), currentUserId);
                return cached.Select(p => { p.IsLikedByCurrentUser = likedIds.Contains(p.Id); return p; }).Skip(skip).Take(take).ToList();
            }
            return cached.Skip(skip).Take(take).ToList();
        }

        var posts = await _context.Posts
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var dtos = posts.Select(p => MapToResponseDto(p, false)).ToList();
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));

        if (currentUserId.HasValue)
        {
            var likedIds = await GetLikedPostIdsAsync(dtos.Select(p => p.Id).ToList(), currentUserId);
            return dtos.Select(p => { p.IsLikedByCurrentUser = likedIds.Contains(p.Id); return p; }).Skip(skip).Take(take).ToList();
        }

        return dtos.Skip(skip).Take(take).ToList();
    }

    public async Task<List<int>> GetAllPostIdsAsync()
    {
        var cacheKey = "posts:ids";
        var cached = await _cache.GetAsync<List<int>>(cacheKey);
        if (cached != null)
            return cached;

        var ids = await _context.Posts
            .Select(p => p.Id)
            .ToListAsync();

        await _cache.SetAsync(cacheKey, ids, TimeSpan.FromMinutes(10));
        return ids;
    }

    public async Task<List<PostResponseDto>> GetPostsByIdsAsync(List<int> ids, int? currentUserId = null)
    {
        var results = new List<PostResponseDto>();
        var misses = new List<int>();
        foreach (var id in ids)
        {
            var cached = await _cache.GetAsync<PostResponseDto>($"post:{id}");
            if (cached != null)
                results.Add(cached);
            else
                misses.Add(id);
        }

        if (misses.Count > 0)
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Where(p => misses.Contains(p.Id))
                .ToListAsync();

            foreach (var p in posts)
            {
                var dto = MapToResponseDto(p, false);
                results.Add(dto);
                await _cache.SetAsync($"post:{p.Id}", dto, TimeSpan.FromMinutes(30));
            }
        }

        var likedIds = await GetLikedPostIdsAsync(results.Select(r => r.Id).ToList(), currentUserId);
        return ids.Where(id => results.Any(r => r.Id == id)).Select(id => {
            var r = results.First(x => x.Id == id);
            r.IsLikedByCurrentUser = likedIds.Contains(id);
            return r;
        }).ToList();
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

    public async Task DeletePostAsync(int postId, int userId, bool isAdmin = false)
    {
        var post = await _context.Posts.FindAsync(postId)
            ?? throw new InvalidOperationException("Post not found.");

        if (!isAdmin && post.UserId != userId)
            throw new UnauthorizedAccessException("You are not allowed to delete this post.");

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"post:{postId}");
        await _cache.RemoveAsync("posts:all");
        await _cache.RemoveAsync("posts:ids");
    }

    public async Task EditPostAsync(int postId, int requestingUserId, EditPostDto editDto, bool isAdmin)
    {
        var post = await _context.Posts.FindAsync(postId)
            ?? throw new InvalidOperationException("Post not found.");

        if (!isAdmin && post.UserId != requestingUserId)
            throw new UnauthorizedAccessException("You are not allowed to edit this post.");

        if (!string.IsNullOrWhiteSpace(editDto.Content))
            post.Content = editDto.Content;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"post:{postId}");
        await _cache.RemoveAsync("posts:all");
    }
}