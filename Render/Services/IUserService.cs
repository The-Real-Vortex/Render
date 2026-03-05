using Render.Shared.Models;

namespace Render.Server.Services;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterUserDto registerDto);
    Task<UserResponseDto> LoginAsync(LoginUserDto loginDto);
    Task<bool> IsUsernameTaken(string username);
    Task<bool> IsEmailTaken(string email);
}
