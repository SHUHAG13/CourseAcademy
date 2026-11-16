using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services.Media;
using FluentValidation;

namespace CourseAcademy.Api.Validators;

public sealed class CreateUploadRequestValidator : AbstractValidator<CreateUploadRequest>
{
    public CreateUploadRequestValidator()
    {
        RuleFor(r => r.CourseId).GreaterThan(0).WithMessage("Choose a course.");
        RuleFor(r => r.Kind).Must(k => k is MediaKindCodes.Video or MediaKindCodes.Document).WithMessage("Choose video or document.");
        RuleFor(r => r.FileName).Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= MediaRules.MaxFileNameLength)
            .WithMessage("The file name can be up to 255 characters.");
        RuleFor(r => r.MimeType).NotEmpty().WithMessage("The file type is missing.");
        RuleFor(r => r.SizeBytes).GreaterThan(0).WithMessage("The file is empty.");
    }
}
