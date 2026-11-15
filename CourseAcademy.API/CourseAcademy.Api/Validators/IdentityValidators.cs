using CourseAcademy.Api.Dtos;
using FluentValidation;

namespace CourseAcademy.Api.Validators;

public static class ValidationPatterns
{
    /// <summary>Bangladeshi mobile number, with or without +88 / 88 prefix.</summary>
    public const string BangladeshPhone = @"^(?:\+?88)?01[3-9]\d{8}$";

    /// <summary>At least one letter and one digit.</summary>
    public const string LetterAndDigit = @"^(?=.*[A-Za-z])(?=.*\d).+$";
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2).WithMessage("Enter your full name.")
            .MaximumLength(100).WithMessage("The name can be up to 100 characters.");
        RuleFor(r => r.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter a valid email address.")
            .MaximumLength(256).WithMessage("Enter a valid email address.")
            .EmailAddress().WithMessage("Enter a valid email address.");
        RuleFor(r => r.Phone)
            .Matches(ValidationPatterns.BangladeshPhone).WithMessage("Enter a valid Bangladeshi mobile number.")
            .When(r => !string.IsNullOrWhiteSpace(r.Phone));
        RuleFor(r => r.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Use at least 8 characters with a letter and a number.")
            .MinimumLength(8).WithMessage("Use at least 8 characters with a letter and a number.")
            .MaximumLength(100).WithMessage("The password can be up to 100 characters.")
            .Matches(ValidationPatterns.LetterAndDigit).WithMessage("Use at least 8 characters with a letter and a number.");
        RuleFor(r => r.AcceptTerms).Equal(true).WithMessage("Please accept the terms to continue.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().WithMessage("Enter your email address.").MaximumLength(256);
        RuleFor(r => r.Password).NotEmpty().WithMessage("Enter your password.").MaximumLength(100);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(r => r.RefreshToken).NotEmpty().MaximumLength(200);
    }
}

public sealed class RoleSaveRequestValidator : AbstractValidator<RoleSaveRequest>
{
    public RoleSaveRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Enter a role name.")
            .MaximumLength(200).WithMessage("The role name can be up to 200 characters.");
        RuleFor(r => r.Description).MaximumLength(500).WithMessage("The description can be up to 500 characters.");
    }
}

public sealed class SetUserRoleRequestValidator : AbstractValidator<SetUserRoleRequest>
{
    public SetUserRoleRequestValidator()
    {
        RuleFor(r => r.RoleCode).NotEmpty().WithMessage("Choose a role.");
    }
}

public sealed class SetCourseGrantRequestValidator : AbstractValidator<SetCourseGrantRequest>
{
    public SetCourseGrantRequestValidator()
    {
        RuleFor(r => r.Permissions).NotNull().WithMessage("Send the list of permissions.");
    }
}

public sealed class SetUserAccessRequestValidator : AbstractValidator<SetUserAccessRequest>
{
    public SetUserAccessRequestValidator()
    {
        RuleFor(r => r.Screens).NotNull().WithMessage("Send the list of screens.");
    }
}
