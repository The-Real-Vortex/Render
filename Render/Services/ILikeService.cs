public interface ILikeService
{
    Task LikePostAsync(int postId, int userId);
    Task UnlikePostAsync(int postId, int userId);
}
