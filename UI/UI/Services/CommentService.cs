using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UI.Models;

namespace UI.Services
{
    /// <summary>Calls ProductService comments endpoint via API Gateway.</summary>
    public sealed class CommentService : ICommentService
    {
        private readonly HttpClient              _http;
        private readonly ILogger<CommentService> _logger;

        public CommentService(HttpClient http, ILogger<CommentService> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task<List<CommentViewModel>> GetCommentsAsync(
            Guid productId, CancellationToken ct = default)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<CommentViewModel>>(
                    $"api/Products/{productId}/comments", ct);
                return result ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load comments for product {ProductId}", productId);
                return new();
            }
        }

        public async Task<(bool Success, string? Error, CommentViewModel? Comment)> PostCommentAsync(
            Guid productId, string commentText, string jwtToken, CancellationToken ct = default)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/Products/{productId}/comments");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);

                request.Content = JsonContent.Create(new { comment = commentText });

                var response = await _http.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content
                        .ReadFromJsonAsync<CommentViewModel>(cancellationToken: ct);
                    return (true, null, data);
                }

                var body    = await response.Content.ReadAsStringAsync(ct);
                var errDoc  = JsonDocument.Parse(body);
                var message = errDoc.RootElement.TryGetProperty("error", out var e)
                              ? e.GetString() : "Could not post comment.";
                return (false, message, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post comment for product {ProductId}", productId);
                return (false, "Unable to post comment. Please try again.", null);
            }
        }
    }
}
