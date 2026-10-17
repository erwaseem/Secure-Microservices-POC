using UI.Models;

namespace UI.Services
{
    public interface ICommentService
    {
        Task<List<CommentViewModel>> GetCommentsAsync(Guid productId, CancellationToken ct = default);
        Task<(bool Success, string? Error, CommentViewModel? Comment)> PostCommentAsync(
            Guid productId, string commentText, string jwtToken, CancellationToken ct = default);
    }
}
