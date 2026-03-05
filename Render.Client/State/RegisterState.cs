using Render.Client.Models;
using MudBlazor;
using System.Net.Http.Json;

namespace Render.Client.State;

public class RegisterState(HttpClient http, ISnackbar snackbar)
{
    public RegisterUserModel UserModel { get; set; } = new RegisterUserModel();

    public async Task RegisterUserAsync()
    {
        var response = await http.PostAsJsonAsync("api/user/register", UserModel.ToDto());
        

        if (response.IsSuccessStatusCode)
        {
            snackbar.Add("Registration successful!", Severity.Success);
            UserModel.Reset();
        }
        else
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            snackbar.Add($"Error: {errorMsg}", Severity.Error);
        }
    }

    public async Task<IEnumerable<string>> ValidateUsernameAsync(string value)
    {
        try
        {
            var response = await http.GetAsync($"api/user/check-username?username={Uri.EscapeDataString(value)}");
            if (response.IsSuccessStatusCode)
            {
                var isTaken = await response.Content.ReadFromJsonAsync<bool>();
                if (isTaken)
                {
                    return new[] { "Username is already taken." };
                }
            }
        }
        catch
        {
            return new[] { "Unable to check username." };
        }
        return Array.Empty<string>();
    }

    public async Task<IEnumerable<string>> ValidateEmailAsync(string value)
    {
        try
        {
            var response = await http.GetAsync($"api/user/check-email?email={Uri.EscapeDataString(value)}");
            if (response.IsSuccessStatusCode)
            {
                var isTaken = await response.Content.ReadFromJsonAsync<bool>();
                if (isTaken)
                {
                    return new[] { "E-Mail is already in use." };
                }
            }
        }
        catch
        {
            return new[] { "Unable to check email." };
        }
        return Array.Empty<string>();
    }
}