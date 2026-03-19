using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Render.Shared.Models;
using Render.Server.Services;
using Render.Data;
using System.Security.Claims;

namespace Render.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly AppDbContext _context;

    public UserController(IUserService userService, AppDbContext context)
    {
        _userService = userService;
        _context = context;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <remarks>
    /// Checks if email and username are unique.
    ///
    /// Example Request:
    ///
    ///     POST /api/user/register
    ///     {
    ///        "username": "NewUser",
    ///        "email": "user@example.com",
    ///        "password": "SecurePassword123!",
    ///        "phoneNumber": "+41 79 000 00 00",
    ///        "bio": "Hello, I'm new here!"
    ///     }
    /// </remarks>
    /// <param name="registerDto">The user registration data.</param>
    /// <returns>The created user including ID and creation date.</returns>
    /// <response code="200">Registration successful. Returns the user.</response>
    /// <response code="400">If email or username are already taken.</response>
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
    /// <remarks>
    /// Checks if email is already in the database.
    ///
    /// Example Request:
    ///
    ///     POST /api/user/login
    ///     {
    ///        "email": "user@example.com",
    ///        "password": "SecurePassword123!"
    ///     }
    /// </remarks>
    /// <param name="loginDto">The user login data.</param>
    /// <returns>The created user including ID and creation date.</returns>
    /// <response code="200">Login successful. Returns the user.</response>
    /// <response code="400">If email is not in the database or password is wrong.</response>
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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), GetAuthenticationProperties(loginDto.RememberMe));

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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}