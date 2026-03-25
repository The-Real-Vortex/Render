using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Render.Shared.Models;
using Render.Server.Services;
using Render.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Render.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public UserController(IUserService userService, AppDbContext context, IWebHostEnvironment env)
    {
        _userService = userService;
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> Register(RegisterUserDto registerDto)
    {
        try
        {
            var result = await _userService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Logs in a user.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> Login(LoginUserDto loginDto)
    {
        try
        {
            var result = await _userService.LoginAsync(loginDto);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()),
                new Claim(ClaimTypes.Name, result.Username),
                new Claim(ClaimTypes.Email, result.Email),
                new Claim(ClaimTypes.Role, result.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                GetAuthenticationProperties(loginDto.RememberMe));

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [NonAction]
    public AuthenticationProperties GetAuthenticationProperties(bool isPersistant = false)
    {
        return new AuthenticationProperties()
        {
            IsPersistent = isPersistant,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
            RedirectUri = "/testpage"
        };
    }

    [HttpGet("check-username")]
    public async Task<bool> CheckUsername(string username)
    {
        return await _userService.IsUsernameTaken(username);
    }

    [HttpGet("check-email")]
    public async Task<bool> CheckEmail(string email)
    {
        return await _userService.IsEmailTaken(email);
    }

    [HttpGet("current-user")]
    public async Task<ActionResult<UserResponseDto?>> GetCurrentUser()
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userId, out var id))
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user != null)
                    {
                        return Ok(new UserResponseDto
                        {
                            Id = user.Id,
                            Username = user.Username,
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber,
                            Bio = user.Bio,
                            CreatedAt = user.CreatedAt,
                            Role = user.Role
                        });
                    }
                }
            }
            return Ok(null);
        }
        catch
        {
            return Ok(null);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    /// <summary>
    /// Promotes a user to Admin role by username (development only).
    /// </summary>
    [HttpPost("make-admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MakeAdmin([FromQuery] string username)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound($"User '{username}' not found.");

        user.Role = "Admin";
        await _context.SaveChangesAsync();

        return Ok($"User '{username}' is now Admin.");
    }
}