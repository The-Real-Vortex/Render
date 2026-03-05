using Render.Client.Models;
using MudBlazor;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Render.Client.State;

public class CreatePostState
{
    private readonly HttpClient http;
    private readonly ISnackbar snackbar;
    private readonly NavigationManager navigationManager;

    public CreatePostState(HttpClient http, ISnackbar snackbar, NavigationManager navigationManager)
    {
        this.http = http;
        this.snackbar = snackbar;
        this.navigationManager = navigationManager;
    }

    public CreatePostModel PostModel { get; set; } = new CreatePostModel();

    public async Task CreatePostAsync()
    {
        var response = await http.PostAsJsonAsync("api/post/create", new Render.Shared.Models.CreatePostDto
        {
            Content = PostModel.Content,
            ImageBytes = PostModel.ImageBytes
        });

        if (response.IsSuccessStatusCode)
        {
            var createdPost = await response.Content.ReadFromJsonAsync<PostResponseDto>();
            
            snackbar.Add("Post created successfully!", Severity.Success);
            navigationManager.NavigateTo($"/post/{createdPost?.Id}");

            PostModel.Reset();
        }
        else
        {
            snackbar.Add("Error: Unable to create post.", Severity.Error);
        }
    }
}