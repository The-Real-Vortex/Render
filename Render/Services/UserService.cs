using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Render.Data;
using Render.Shared;
using Render.Shared.Models;
using System.Security.Claims;

namespace Render.Server.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IShardedCache _cache;

    public UserService(AppDbContext context, IShardedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterUserDto registerDto)
    {
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            PhoneNumber = registerDto.PhoneNumber,
            Bio = registerDto.Bio,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Invalidate caches that might list users
        await _cache.RemoveAsync($"user:{user.Id}");
        await _cache.RemoveAsync("users:all");
        return MapToResponseDto(user);
    }

    public async Task<UserResponseDto> LoginAsync(LoginUserDto loginDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            throw new InvalidOperationException("Invalid email or password.");

        var dto = MapToResponseDto(user);
        // Cache user DTO by id and email for faster subsequent lookups
        await _cache.SetAsync($"user:{user.Id}", dto, TimeSpan.FromMinutes(30));
        await _cache.SetAsync($"user:email:{user.Email}", dto, TimeSpan.FromMinutes(30));
        return dto;
    }

    public async Task<bool> IsUsernameTaken(string username)
    {
        var cacheKey = $"user:username:{username}";
        var cached = await _cache.GetAsync<bool?>(cacheKey);
        if (cached.HasValue)
            return cached.Value;

        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        await _cache.SetAsync(cacheKey, exists, TimeSpan.FromMinutes(10));
        return exists;
    }

    public async Task<bool> IsEmailTaken(string email)
    {
        var cacheKey = $"user:emailtaken:{email}";
        var cached = await _cache.GetAsync<bool?>(cacheKey);
        if (cached.HasValue)
            return cached.Value;

        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        await _cache.SetAsync(cacheKey, exists, TimeSpan.FromMinutes(10));
        return exists;
    }

    private static UserResponseDto MapToResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            Role = user.Role
        };
    }
}