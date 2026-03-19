using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Render.Shared.Models;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Render.Server.Controllers;

using Render.Data;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public PostController(IPostService postService, AppDbContext context, IMemoryCache cache)
    {
        _postService = postService;
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Creates a new post.
    /// </summary>
    /// <remarks>
    /// Creates a new post.
    ///
    /// Example Request:
    ///
    ///     POST /api/post/create
    ///     {
    ///        "content": "This is a new post."
    ///     }
    /// </remarks>
    /// <param name="postDto">The post creation data.</param>
    /// <returns>The created post including ID and creation date.</returns>
    /// <response code="200">Post creation successful. Returns the post.</response>
    /// <response code="400">If there is an error during post creation.</response>
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostResponseDto>> CreatePostAsync(CreatePostDto postDto)
    {

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("You must be logged in to create a post.");
        }

        try
        {
            var result = await _postService.CreatePostAsync(postDto, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponseDto>> GetPostByIdAsync(int id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id, GetCurrentUserId());
            return Ok(post);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<PostResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostResponseDto>>> GetPostsAsync(
        [FromQuery] int take = 10,
        [FromQuery] int skip = 0,
        [FromQuery] Guid? sessionId = null)
    {
        if (sessionId == null || sessionId == Guid.Empty)
        {
            var posts = await _postService.GetPostsAsync(take, skip, GetCurrentUserId());
            return Ok(posts);
        }

        var cacheKey = $"shuffle_{sessionId}";

        if (!_cache.TryGetValue(cacheKey, out List<int>? shuffledIds))
        {
            var allIds = await _postService.GetAllPostIdsAsync();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(allIds));
            shuffledIds = allIds;

            _cache.Set(cacheKey, shuffledIds, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }

        var pageIds = shuffledIds!
            .Skip(skip)
            .Take(take)
            .ToList();

        if (pageIds.Count == 0)
        {
            return Ok(new List<PostResponseDto>());
        }

        var result = await _postService.GetPostsByIdsAsync(pageIds, GetCurrentUserId());
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out int id) ? id : null;
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePostAsync(int id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _postService.DeletePostAsync(id, userId, isAdmin);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> EditPostAsync(int id, [FromBody] EditPostDto editDto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _postService.EditPostAsync(id, userId, editDto, isAdmin);
            return Ok();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException) { return NotFound(); }
    }
}