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
    /// Seeds the database with dummy posts (development only).
    /// </summary>
    /// <param name="count">Number of posts to generate (default: 50, max: 1000).</param>
    /// <returns>The number of posts created.</returns>
    /// <response code="200">Returns the number of posts created.</response>
    /// <response code="400">If no users exist in the database.</response>
    /// <response code="403">If called outside of the development environment.</response>
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
}
