namespace CourseAcademy.Api.Entities;

/// <summary>
/// A user's progress on one lesson. A lesson is a video, or a section that has no videos (BR-LRN-15).
/// One row per user and lesson: <see cref="SectionVideoId"/> is null for a section without videos.
/// </summary>
public sealed class LessonProgress : BaseEntity
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public int SectionId { get; set; }
    public int? SectionVideoId { get; set; }

    /// <summary>Where the user stopped watching, for resuming. Only for videos whose position can be read (BR-LRN-17).</summary>
    public int PositionSeconds { get; set; }
    /// <summary>Furthest point reached, used for automatic completion.</summary>
    public int MaxWatchedSeconds { get; set; }
    /// <summary>Video length as reported by the player.</summary>
    public int? DurationSeconds { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}
