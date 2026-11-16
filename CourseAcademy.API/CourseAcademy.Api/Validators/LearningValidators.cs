using CourseAcademy.Api.Dtos;
using FluentValidation;

namespace CourseAcademy.Api.Validators;

public sealed class VideoPositionRequestValidator : AbstractValidator<VideoPositionRequest>
{
    /// <summary>One day. Longer values are not real lesson videos.</summary>
    public const int MaxSeconds = 86_400;

    public VideoPositionRequestValidator()
    {
        RuleFor(r => r.DurationSeconds).InclusiveBetween(1, MaxSeconds).WithMessage("The video length is not valid.");
        RuleFor(r => r.PositionSeconds).InclusiveBetween(0, MaxSeconds).WithMessage("The video position is not valid.");
    }
}
