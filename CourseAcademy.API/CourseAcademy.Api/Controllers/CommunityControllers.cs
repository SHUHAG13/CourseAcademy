using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission and course-access checks are made in the services.

/// <summary>Course discussion boards: posts, comments, replies and moderation.</summary>
[Authorize]
[Route("api/v1")]
public sealed class CommunityController(ICommunityService community) : ApiControllerBase
{
    /// <summary>What the caller may do, and the public post count. Visitors get the count only.</summary>
    [HttpGet("courses/{courseSlug}/community")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<CommunityInfoDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Info(string courseSlug, CancellationToken ct) => Success(await community.GetInfoAsync(courseSlug, ct));

    [HttpGet("courses/{courseSlug}/posts")]
    [ProducesResponseType<ApiResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Posts(string courseSlug, [FromQuery] PageQuery page, CancellationToken ct) =>
        Success(await community.GetPostsAsync(courseSlug, page, ct));

    [HttpPost("courses/{courseSlug}/posts")]
    [ProducesResponseType<ApiResponse<PostDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePost(string courseSlug, PostSaveRequest request, CancellationToken ct) =>
        Created(await community.CreatePostAsync(courseSlug, request, ct));

    [HttpPut("posts/{id:int}")]
    [ProducesResponseType<ApiResponse<PostDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePost(int id, PostSaveRequest request, CancellationToken ct) =>
        Success(await community.UpdatePostAsync(id, request, ct));

    [HttpDelete("posts/{id:int}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePost(int id, CancellationToken ct)
    {
        await community.DeletePostAsync(id, ct);
        return Success();
    }

    [HttpPatch("posts/{id:int}/flags")]
    [ProducesResponseType<ApiResponse<PostDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetFlags(int id, PostFlagsRequest request, CancellationToken ct) =>
        Success(await community.SetPostFlagsAsync(id, request, ct));

    [HttpGet("posts/{id:int}/comments")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CommentDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Comments(int id, CancellationToken ct) => Success(await community.GetCommentsAsync(id, ct));

    [HttpPost("posts/{id:int}/comments")]
    [ProducesResponseType<ApiResponse<CommentDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(int id, CommentAddRequest request, CancellationToken ct) =>
        Created(await community.AddCommentAsync(id, request, ct));

    [HttpPut("comments/{id:int}")]
    [ProducesResponseType<ApiResponse<CommentDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateComment(int id, CommentEditRequest request, CancellationToken ct) =>
        Success(await community.UpdateCommentAsync(id, request, ct));

    [HttpDelete("comments/{id:int}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct)
    {
        await community.DeleteCommentAsync(id, ct);
        return Success();
    }

    [HttpPatch("comments/{id:int}/hidden")]
    [ProducesResponseType<ApiResponse<CommentDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCommentHidden(int id, CommentHiddenRequest request, CancellationToken ct) =>
        Success(await community.SetCommentHiddenAsync(id, request.IsHidden, ct));

    [HttpGet("admin/community/posts")]
    [ProducesResponseType<ApiResponse<PagedResult<ModerationPostDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ModerationPosts([FromQuery] ModerationPostQuery query, CancellationToken ct) =>
        Success(await community.GetModerationPostsAsync(query, ct));

    /// <summary>The newest comments (20 unless a limit is given).</summary>
    [HttpGet("admin/community/comments")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ModerationCommentDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ModerationComments([FromQuery] ModerationCommentQuery query, CancellationToken ct) =>
        Success(await community.GetModerationCommentsAsync(query, ct));
}
