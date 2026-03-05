using Render.Shared.Models;

public interface IPostService
{
    Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto, int userId);
    Task<PostResponseDto> GetPostByIdAsync(int postId, int? currentUserId = null);
    Task<List<PostResponseDto>> GetPostsAsync(int take = 10, int skip = 0, int? currentUserId = null);
    Task<List<int>> GetAllPostIdsAsync();
    Task<List<PostResponseDto>> GetPostsByIdsAsync(List<int> ids, int? currentUserId = null);
    Task DeletePostAsync(int postId, int userId);
}