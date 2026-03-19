using Render.Client.Models;
using MudBlazor;
using System.Net.Http.Json;
using Render.Shared.Models;

namespace Render.Client.State;

public class LoginState(HttpClient http, ISnackbar snackbar)
{
    public LoginUserModel UserModel { get; set; } = new LoginUserModel();
    public UserResponseDto? CurrentUser { get; private set; }
    public bool RememberMe { get; set; } = false;
    public bool IsInitialized { get; private set; } = false;

    public event Action? OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task LoginUserAsync()
    {
        var response = await http.PostAsJsonAsync($"api/user/login?isPersistant={this.RememberMe}", new Render.Shared.Models.LoginUserDto
        {
            Email = UserModel.Email,
            Password = UserModel.Password,
            RememberMe = RememberMe
        });

        if (response.IsSuccessStatusCode)
        {
            CurrentUser = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            snackbar.Add("Login successful!", Severity.Success);
            UserModel.Reset();
            NotifyStateChanged();
        }
        else
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            snackbar.Add($"Error: Please sign in with valid credentials", Severity.Error);
        }
    }

    public bool IsAuthenticated()
    {
        return CurrentUser != null;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        if (IsInitialized)
        {
            return CurrentUser != null;
        }

        try
        {
            var response = await http.GetAsync("api/user/current-user");
            if (response.IsSuccessStatusCode)
            {
                CurrentUser = await response.Content.ReadFromJsonAsync<UserResponseDto>();
                if (CurrentUser != null)
                {
                    snackbar.Add($"Welcome back, {CurrentUser.Username}!", Severity.Success);
                    NotifyStateChanged();
                    return true;
                }
            }
        }
        catch
        {
        }
        finally
        {
            IsInitialized = true;
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await http.PostAsync("api/user/logout", null);
        }
        catch
        {
            snackbar.Add("Failed to log out", Severity.Error);
        }

        CurrentUser = null;
        snackbar.Add("Logged out successfully", Severity.Info);
        NotifyStateChanged();
    }

    public async Task<IEnumerable<string>> ValidateEmailAsync(string value)
    {
        try
        {
            var response = await http.GetAsync($"api/user/check-email?email={Uri.EscapeDataString(value)}");
            if (response.IsSuccessStatusCode)
            {
                var isTaken = await response.Content.ReadFromJsonAsync<bool>();
                if (!isTaken)
                {
                    return new[] { "There is no account with this email." };
                }
            }
        }
        catch
        {
            return new[] { "Unable to check email." };
        }
        return Array.Empty<string>();
    }

    public bool IsAdmin() => CurrentUser?.Role == "Admin";
}