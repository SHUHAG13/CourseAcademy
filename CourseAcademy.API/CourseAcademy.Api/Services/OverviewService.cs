using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Api.Services;

public interface IOverviewService
{
    /// <summary>Totals and the latest payments for the admin overview (needs <c>reports.view</c>).</summary>
    Task<AdminOverviewDto> GetAsync(CancellationToken ct);
}

public sealed class OverviewService(IOverviewRepository overview, ICurrentUser currentUser) : IOverviewService
{
    public const int RecentPayments = 5;

    public async Task<AdminOverviewDto> GetAsync(CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.ReportsView, null, ct);
        var counts = await overview.GetCountsAsync(ct);
        var recent = await overview.ListRecentPaymentsAsync(RecentPayments, ct);
        return new AdminOverviewDto(
            counts.Courses, counts.PublishedCourses, counts.Students, counts.ActiveEnrollments, counts.Collected,
            recent.Select(t => new RecentTransactionDto(t.TransactionNo, t.CreatedAt, t.UserName, t.CourseTitle, t.Amount, t.Status.Code)).ToList());
    }
}
