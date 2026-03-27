// AI GENERATED
using Microsoft.AspNetCore.Mvc;
using Render.Data;

namespace Render.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SeedController : ControllerBase
{
    private readonly DataSeeder _seeder;
    private readonly IWebHostEnvironment _env;

    public SeedController(DataSeeder seeder, IWebHostEnvironment env)
    {
        _seeder = seeder;
        _env = env;
    }

    /// <summary>
    /// Seeds the database with dummy users (development only).
    /// Password for all seeded users: Seed_1234!
    /// </summary>
    /// <param name="count">Number of users to generate (default: 10, max: 1000).</param>
    [HttpPost("users")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedUsers([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var created = await _seeder.SeedUsersAsync(count, cancellationToken);
        return Ok($"{created} users seeded successfully. Password: Seed_1234!");
    }

    /// <summary>
    /// Seeds the database with dummy posts (development only).
    /// </summary>
    /// <param name="count">Number of posts to generate (default: 50, max: 1000).</param>
    [HttpPost("posts")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedPosts([FromQuery] int count = 50, CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        count = Math.Clamp(count, 1, 1000);

        try
        {
            var created = await _seeder.SeedPostsAsync(count, cancellationToken);
            return Ok($"{created} posts seeded successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Seeds both users and posts in one call (development only).
    /// </summary>
    /// <param name="userCount">Number of users to seed (default: 10, max: 1000).</param>
    /// <param name="postCount">Number of posts to seed (default: 50, max: 1000).</param>
    [HttpPost("all")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedAll(
        [FromQuery] int userCount = 10,
        [FromQuery] int postCount = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var users = await _seeder.SeedUsersAsync(userCount, cancellationToken);

        try
        {
            var posts = await _seeder.SeedPostsAsync(postCount, cancellationToken);
            return Ok($"{users} users and {posts} posts seeded successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}