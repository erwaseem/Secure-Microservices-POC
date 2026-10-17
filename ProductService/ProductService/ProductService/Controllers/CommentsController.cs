using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;
using System.Security.Claims;

namespace ProductService.Controllers
{
    [Route("api/Products/{productId:guid}/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly ProductDb _db;

        public CommentsController(ProductDb db) => _db = db;

        //  GET /api/Products/{productId}/comments 
        [HttpGet]
        public async Task<IActionResult> GetComments(Guid productId, CancellationToken ct)
        {
            var comments = await _db.ProductComments
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedUtc)
                .Select(c => new
                {
                    c.Id,
                    c.Username,
                    c.Comment,
                    c.CreatedUtc
                })
                .ToListAsync(ct);

            return Ok(comments);
        }

        //  POST /api/Products/{productId}/comments 
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(
            Guid productId,
            [FromBody] AddCommentDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest(new { error = "Comment cannot be empty." });

            var productExists = await _db.Products.AnyAsync(p => p.Id == productId, ct);
            if (!productExists) return NotFound(new { error = "Product not found." });

            var username = User.FindFirstValue(ClaimTypes.Name) ?? "Anonymous";

            var comment = new ProductComment
            {
                ProductId  = productId,
                Username   = username,
                Comment    = dto.Comment.Trim(),
                CreatedUtc = DateTime.UtcNow
            };

            _db.ProductComments.Add(comment);
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                comment.Id,
                comment.Username,
                comment.Comment,
                comment.CreatedUtc
            });
        }
    }

    public record AddCommentDto(string Comment);
}
