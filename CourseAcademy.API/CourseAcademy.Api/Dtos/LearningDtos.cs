namespace CourseAcademy.Api.Dtos;

// ── Progress ────────────────────────────────────────────────────

/// <summary>A started video: where to resume, and whether it is done.</summary>
public sealed record VideoProgressDto(int VideoId, int PositionSeconds, int? DurationSeconds, bool IsCompleted);

/// <summary>
/// The signed-in user's progress in one course. Lessons are videos plus sections without videos (BR-LRN-15).
/// CompletedSectionIds lists sections whose lessons are all done.
/// </summary>
public sealed record CourseProgressDto(
    int CourseId, int CompletedLessons, int TotalLessons, int Percent,
    IReadOnlyList<int> CompletedSectionIds, IReadOnlyList<VideoProgressDto> Videos);

public sealed record CourseProgressSummaryDto(int CourseId, int CompletedLessons, int TotalLessons, int Percent);

public sealed record SetCompletedRequest(bool Completed);

/// <summary>Sent by the player while a video plays. Seconds are whole numbers.</summary>
public sealed record VideoPositionRequest(int PositionSeconds, int DurationSeconds);

// ── Admin overview ──────────────────────────────────────────────

public sealed record RecentTransactionDto(string TransactionNo, DateTime CreatedAt, string UserName, string CourseTitle, decimal Amount, string StatusCode);

public sealed record AdminOverviewDto(
    int CourseCount, int PublishedCourseCount, int StudentCount, int ActiveEnrollmentCount, decimal CollectedAmount,
    IReadOnlyList<RecentTransactionDto> RecentTransactions);
