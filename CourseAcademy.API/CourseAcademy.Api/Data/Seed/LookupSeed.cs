using CourseAcademy.Api.Entities;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Fixed lookup values, inserted by migrations. Codes match the Angular app's values exactly.
/// Ids are fixed so migrations stay stable; never renumber an existing row.
/// </summary>
public static class LookupSeed
{
    /// <summary>Seed rows need a constant timestamp, otherwise every migration would rewrite them.</summary>
    private static readonly DateTime SeededAt = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static T Row<T>(int id, string code, string name, Action<T>? extra = null) where T : LookupEntity, new()
    {
        var row = new T { Id = id, Code = code, Name = name, SortOrder = id, CreatedAt = SeededAt };
        extra?.Invoke(row);
        return row;
    }

    public static readonly CourseFormat[] CourseFormats =
    [
        Row<CourseFormat>(1, "Pre-recorded", "Pre-recorded"),
        Row<CourseFormat>(2, "Live", "Live"),
        Row<CourseFormat>(3, "Hybrid", "Hybrid"),
    ];

    public static readonly CourseLevel[] CourseLevels =
    [
        Row<CourseLevel>(1, "Beginner", "Beginner"),
        Row<CourseLevel>(2, "Intermediate", "Intermediate"),
        Row<CourseLevel>(3, "Advanced", "Advanced"),
    ];

    public static readonly CourseBadge[] CourseBadges =
    [
        Row<CourseBadge>(1, "Bestseller", "Bestseller"),
        Row<CourseBadge>(2, "New", "New"),
        Row<CourseBadge>(3, "Popular", "Popular"),
    ];

    public static readonly DocumentType[] DocumentTypes =
    [
        Row<DocumentType>(1, "PDF", "PDF", d => d.FileExtension = ".pdf"),
        Row<DocumentType>(2, "DOCX", "Word document", d => d.FileExtension = ".docx"),
        Row<DocumentType>(3, "PPTX", "PowerPoint slides", d => d.FileExtension = ".pptx"),
    ];

    public static readonly MaterialType[] MaterialTypes =
    [
        Row<MaterialType>(1, "PDF", "PDF"),
        Row<MaterialType>(2, "Slides", "Slides"),
        Row<MaterialType>(3, "Worksheet", "Worksheet"),
        Row<MaterialType>(4, "Link", "Link"),
    ];

    public static readonly EnrollmentStatus[] EnrollmentStatuses =
    [
        Row<EnrollmentStatus>(1, "active", "Active"),
        Row<EnrollmentStatus>(2, "revoked", "Revoked"),
    ];

    public static readonly PaymentStatus[] PaymentStatuses =
    [
        Row<PaymentStatus>(1, "pending", "Pending"),
        Row<PaymentStatus>(2, "success", "Success"),
        Row<PaymentStatus>(3, "failed", "Failed"),
        Row<PaymentStatus>(4, "cancelled", "Cancelled"),
        Row<PaymentStatus>(5, "refunded", "Refunded"),
    ];

    public static readonly Currency[] Currencies =
    [
        Row<Currency>(1, "BDT", "Bangladeshi Taka"),
    ];

    public static readonly Category[] Categories =
    [
        Row<Category>(1, "video-editing", "Video Editing", c =>
        {
            c.ShortName = "Video Editing";
            c.Description = "Edit, colour-grade and publish professional videos for YouTube, brands and broadcast.";
            c.Icon = "bi-camera-reels";
            c.ImageUrl = "/images/web/creative-studio.webp";
        }),
        Row<Category>(2, "digital-marketing", "Digital Marketing", c =>
        {
            c.ShortName = "Marketing";
            c.Description = "Plan campaigns, run paid ads and measure the growth they bring to a real business.";
            c.Icon = "bi-megaphone";
            c.ImageUrl = "/images/web/contact-bg.webp";
        }),
        Row<Category>(3, "sports-journalism", "Sports Journalism", c =>
        {
            c.ShortName = "Sports Journalism";
            c.Description = "Report, write and present sport like a professional — from match reports to live coverage.";
            c.Icon = "bi-mic";
            c.ImageUrl = "/images/web/pitchside-camera.webp";
        }),
        Row<Category>(4, "software-development", "Software Development", c =>
        {
            c.ShortName = "Development";
            c.Description = "Build real websites and apps with modern tools used by software teams today.";
            c.Icon = "bi-code-slash";
            c.ImageUrl = "/images/web/dev-team.webp";
        }),
    ];

    public static readonly CategorySkill[] CategorySkills =
    [
        .. new (int CategoryId, string[] Skills)[]
        {
            (1, ["Premiere Pro", "DaVinci Resolve", "After Effects"]),
            (2, ["SEO", "Meta Ads", "Analytics"]),
            (3, ["Match reporting", "Interviewing", "Sports video"]),
            (4, ["TypeScript", "Angular", "ASP.NET Core"]),
        }.SelectMany(c => c.Skills.Select((skill, index) => new CategorySkill
        {
            Id = (c.CategoryId - 1) * 3 + index + 1, CategoryId = c.CategoryId, Skill = skill, SortOrder = index + 1, CreatedAt = SeededAt,
        })),
    ];

    public static readonly PaymentMethod[] PaymentMethods =
    [
        Row<PaymentMethod>(1, "bkash", "bKash", m =>
        {
            m.Description = "Pay from your bKash wallet.";
            m.LogoUrl = "/logos/bkash.jpg";
            m.Icon = "bi-wallet2";
        }),
        Row<PaymentMethod>(2, "nagad", "Nagad", m =>
        {
            m.Description = "Pay from your Nagad wallet.";
            m.LogoUrl = "/logos/nagad.png";
            m.Icon = "bi-wallet2";
        }),
        Row<PaymentMethod>(3, "rocket", "Rocket", m =>
        {
            m.Description = "Pay from your Rocket account.";
            m.Icon = "bi-rocket-takeoff";
        }),
        Row<PaymentMethod>(4, "card", "Visa / Mastercard", m =>
        {
            m.Description = "Pay by debit or credit card.";
            m.Icon = "bi-credit-card-2-front";
        }),
    ];
}
