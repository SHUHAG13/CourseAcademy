using System.Text.RegularExpressions;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using FluentValidation;

namespace CourseAcademy.Api.Validators;

public static partial class CatalogPatterns
{
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public static partial Regex Slug();

    /// <summary>Same rule as the frontend's URL_PATTERN.</summary>
    [GeneratedRegex(@"^https?://[^\s.]+\.[^\s]{2,}$", RegexOptions.IgnoreCase)]
    public static partial Regex Url();

    /// <summary>An http(s) address or a site path starting with "/".</summary>
    [GeneratedRegex(@"^(?:/[^\s/]|https?://)\S*$", RegexOptions.IgnoreCase)]
    public static partial Regex Image();

    public static bool IsUrl(string? value) => value is not null && value.Length <= 2048 && Url().IsMatch(value.Trim());
}

public sealed class CourseUpsertRequestValidator : AbstractValidator<CourseUpsertRequest>
{
    public CourseUpsertRequestValidator()
    {
        RuleFor(r => r.Title).Must(t => t?.Trim().Length is >= 5 and <= 120).WithMessage("Enter a title of 5 to 120 characters.");
        RuleFor(r => r.Slug).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Use lowercase letters, numbers and hyphens.")
            .MaximumLength(200).WithMessage("The slug can be up to 200 characters.")
            .Matches(CatalogPatterns.Slug()).WithMessage("Use lowercase letters, numbers and hyphens.");
        RuleFor(r => r.Subtitle).Must(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 200)
            .WithMessage("Enter a short description of up to 200 characters.");
        RuleFor(r => r.Description).Cascade(CascadeMode.Stop)
            .Must(d => !string.IsNullOrWhiteSpace(d)).WithMessage("Enter a description.")
            .MaximumLength(8000).WithMessage("The description can be up to 8000 characters.");
        RuleFor(r => r.CategoryCode).NotEmpty().WithMessage("Choose a category.");
        RuleFor(r => r.FormatCode).NotEmpty().WithMessage("Choose a format.");
        RuleFor(r => r.LevelCode).NotEmpty().WithMessage("Choose a level.");
        RuleFor(r => r.InstructorId).GreaterThan(0).WithMessage("Choose an instructor.");
        RuleFor(r => r.Language).Must(l => !string.IsNullOrWhiteSpace(l) && l.Trim().Length <= 50).WithMessage("Enter the language.");
        RuleFor(r => r.DurationHours).GreaterThanOrEqualTo(1).WithMessage("Enter at least 1 hour.");
        RuleFor(r => r.OriginalPrice).GreaterThanOrEqualTo(0).WithMessage("Enter 0 or more.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Use at most 2 decimal places.");
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0).WithMessage("Enter 0 or more.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Use at most 2 decimal places.");
        RuleFor(r => r.Price).LessThanOrEqualTo(r => r.OriginalPrice).WithMessage("The sale price can't be above the regular price.");
        RuleFor(r => r.CoverLabel).Must(l => !string.IsNullOrWhiteSpace(l) && l.Trim().Length <= 30).WithMessage("Enter a label of up to 30 characters.");
        RuleFor(r => r.CoverIcon).Must(i => !string.IsNullOrWhiteSpace(i) && i.Trim().Length <= 50).WithMessage("Enter an icon name.");
        RuleFor(r => r.ImageUrl)
            .Must(i => i!.Trim().Length <= 2048 && CatalogPatterns.Image().IsMatch(i.Trim()))
            .WithMessage("Enter an http(s) address or a site path starting with /.")
            .When(r => !string.IsNullOrWhiteSpace(r.ImageUrl));
        RuleFor(r => r.Outcomes).Must(o => o is not null && o.Any(x => !string.IsNullOrWhiteSpace(x)))
            .WithMessage("Add at least one learning objective.");
        RuleForEach(r => r.Outcomes).MaximumLength(500).WithMessage("Each line can be up to 500 characters.");
        RuleForEach(r => r.Requirements).MaximumLength(500).WithMessage("Each line can be up to 500 characters.");

        RuleForEach(r => r.Schedule).ChildRules(s =>
        {
            s.RuleFor(x => x.Title).Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length <= 200).WithMessage("Enter a session title.");
            s.RuleFor(x => x.StartsAt).Must(v => BangladeshTime.TryParseToUtc(v, out _)).WithMessage("Choose a start date and time.");
            s.RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(1).WithMessage("Enter 1 minute or more.");
            s.RuleFor(x => x.MeetingUrl).Must(CatalogPatterns.IsUrl).WithMessage("Enter a valid http(s) link.");
        });
        RuleForEach(r => r.Materials).ChildRules(m =>
        {
            m.RuleFor(x => x.Title).Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length <= 200).WithMessage("Enter a material title.");
            m.RuleFor(x => x.TypeCode).NotEmpty().WithMessage("Choose a material type.");
            m.RuleFor(x => x.Url).Must(CatalogPatterns.IsUrl).WithMessage("Enter a valid http(s) link.")
                .When(x => x.TypeCode == "Link" || !string.IsNullOrWhiteSpace(x.Url));
        });
    }
}

public sealed class InstructorSaveRequestValidator : AbstractValidator<InstructorSaveRequest>
{
    public InstructorSaveRequestValidator()
    {
        RuleFor(r => r.Name).Must(n => n?.Trim().Length is >= 2 and <= 100).WithMessage("Enter a name.");
        RuleFor(r => r.Title).Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length <= 200).WithMessage("Enter a title.");
        RuleFor(r => r.Bio).Must(b => !string.IsNullOrWhiteSpace(b) && b.Trim().Length <= 4000).WithMessage("Enter a short biography.");
        RuleForEach(r => r.Expertise).MaximumLength(100).WithMessage("Each skill can be up to 100 characters.");
        RuleFor(r => r.ExperienceYears).GreaterThanOrEqualTo(0).WithMessage("Enter 0 or more.");
        RuleFor(r => r.Students).GreaterThanOrEqualTo(0).WithMessage("Enter 0 or more.");
    }
}

public sealed class CurriculumRequestValidator : AbstractValidator<CurriculumRequest>
{
    public CurriculumRequestValidator()
    {
        RuleFor(r => r.Modules).NotNull().WithMessage("Send the list of modules.");
    }
}
