using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

public class AccessServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Screen_access_replaces_the_role_and_gives_whole_screens()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        var studentId = kit.UserId(IdentityTestKit.Student);

        var result = await kit.Access.SetUserAccessAsync(studentId, new SetUserAccessRequest(["instructors", "dashboard"]), Ct);

        Assert.Equal(AccessTypes.Custom, result.AccessType);
        Assert.Equal(["instructors", "dashboard"], result.Screens);
        Assert.Equal(["instructors.view", "instructors.create", "instructors.edit", "instructors.delete", "dashboard.view"], result.Permissions);

        var me = await kit.As(IdentityTestKit.Student).Auth.GetMeAsync(Ct);
        Assert.True(me.HasAdminPanel);
        Assert.Equal("/admin/instructors", me.HomeUrl);
        Assert.DoesNotContain("learn.view", me.Permissions);
    }

    [Fact]
    public async Task Reset_returns_the_user_to_the_role_screens()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        var studentId = kit.UserId(IdentityTestKit.Student);
        await kit.Access.SetUserAccessAsync(studentId, new SetUserAccessRequest(["users"]), Ct);

        var result = await kit.As(IdentityTestKit.Admin).Access.ResetUserAccessAsync(studentId, Ct);

        Assert.Equal(AccessTypes.Role, result.AccessType);
        Assert.Equal(["dashboard", "learn", "checkout", "community"], result.Screens);
        Assert.Empty(await kit.Db.UserScreens.ToListAsync());
    }

    [Fact]
    public async Task Nobody_can_change_their_own_access_or_an_admins()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        var adminId = kit.UserId(IdentityTestKit.Admin);

        var self = await IdentityTestKit.Throws<ConflictException>(() =>
            kit.Access.SetUserAccessAsync(adminId, new SetUserAccessRequest([]), Ct));
        Assert.Equal("You can't change your own access.", self.Message);

        var role = await IdentityTestKit.Throws<ConflictException>(() =>
            kit.Access.SetUserRoleAsync(adminId, new SetUserRoleRequest("user"), Ct));
        Assert.Equal("You can't change your own role.", role.Message);
    }

    [Fact]
    public async Task Admin_users_cannot_get_custom_access()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        var studentId = kit.UserId(IdentityTestKit.Student);
        await kit.Access.SetUserRoleAsync(studentId, new SetUserRoleRequest("admin"), Ct);

        var error = await IdentityTestKit.Throws<ConflictException>(() =>
            kit.As(IdentityTestKit.Admin).Access.SetUserAccessAsync(studentId, new SetUserAccessRequest([]), Ct));

        Assert.Equal("Admin users always have full access.", error.Message);
    }

    [Fact]
    public async Task Users_without_permission_are_refused()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Student);

        await IdentityTestKit.Throws<ForbiddenException>(() => kit.Access.GetUsersAsync(new UserListQuery(), Ct));
        await IdentityTestKit.Throws<ForbiddenException>(() => kit.Access.GetRolesAsync(Ct));
        await IdentityTestKit.Throws<ForbiddenException>(() => kit.Access.CreateRoleAsync(new RoleSaveRequest("X", null, []), Ct));
        await IdentityTestKit.Throws<ForbiddenException>(() =>
            kit.Access.SetCourseGrantAsync(kit.UserId(IdentityTestKit.Learner), 1, new SetCourseGrantRequest(["courses.view"]), Ct));
    }

    [Fact]
    public async Task Course_grants_accept_only_course_permissions_and_empty_removes_them()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        var instructorId = kit.UserId(IdentityTestKit.Instructor);

        var granted = await kit.Access.SetCourseGrantAsync(instructorId, 4, new SetCourseGrantRequest(["courses.edit", "courses.view", "courses.edit"]), Ct);
        Assert.Equal([new CourseGrantDto(4, ["courses.edit", "courses.view"])], granted.CourseGrants, new GrantComparer());

        await IdentityTestKit.Throws<BusinessRuleException>(() =>
            kit.As(IdentityTestKit.Admin).Access.SetCourseGrantAsync(instructorId, 4, new SetCourseGrantRequest(["courses.delete"]), Ct));

        var instructor = await kit.As(IdentityTestKit.Instructor).Auth.GetMeAsync(Ct);
        Assert.True(instructor.HasAdminPanel);
        Assert.Equal("/admin/courses", instructor.HomeUrl);

        var removed = await kit.As(IdentityTestKit.Admin).Access.SetCourseGrantAsync(instructorId, 4, new SetCourseGrantRequest([]), Ct);
        Assert.Empty(removed.CourseGrants);
        Assert.Empty(await kit.Db.CourseGrants.ToListAsync());
        Assert.Empty(await kit.Db.CourseGrantPermissions.ToListAsync());
    }

    [Fact]
    public async Task Roles_are_unique_by_name_and_built_in_or_used_roles_cannot_be_deleted()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);

        var support = await kit.Access.CreateRoleAsync(new RoleSaveRequest(" Support ", " Helps students ", ["enrollments"]), Ct);
        Assert.Equal("Support", support.Name);
        Assert.Equal(["enrollments.view", "enrollments.edit"], support.Permissions);
        Assert.False(support.IsSystem);

        await IdentityTestKit.Throws<ConflictException>(() => kit.Access.CreateRoleAsync(new RoleSaveRequest("SUPPORT", null, []), Ct));
        await IdentityTestKit.Throws<ConflictException>(() => kit.Access.DeleteRoleAsync("instructor", Ct));

        await kit.Access.SetUserRoleAsync(kit.UserId(IdentityTestKit.Learner), new SetUserRoleRequest(support.Code), Ct);
        var inUse = await IdentityTestKit.Throws<ConflictException>(() => kit.As(IdentityTestKit.Admin).Access.DeleteRoleAsync(support.Code, Ct));
        Assert.Equal("Move the users with this role to another role first.", inUse.Message);

        await kit.As(IdentityTestKit.Admin).Access.SetUserRoleAsync(kit.UserId(IdentityTestKit.Learner), new SetUserRoleRequest("user"), Ct);
        await kit.As(IdentityTestKit.Admin).Access.DeleteRoleAsync(support.Code, Ct);
        Assert.DoesNotContain(await kit.Access.GetRolesAsync(Ct), r => r.Code == support.Code);
    }

    [Fact]
    public async Task The_admin_role_always_lists_every_screen()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);

        var admin = await kit.Access.UpdateRoleAsync("admin", new RoleSaveRequest("Administrator", null, ["dashboard"]), Ct);

        Assert.Equal("Administrator", admin.Name);
        Assert.Equal(14, admin.Screens.Count);
        Assert.Equal(27, admin.Permissions.Count);
    }

    [Fact]
    public async Task Unknown_screens_are_rejected()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);

        var error = await IdentityTestKit.Throws<BusinessRuleException>(() =>
            kit.Access.SetUserAccessAsync(kit.UserId(IdentityTestKit.Student), new SetUserAccessRequest(["nope"]), Ct));

        Assert.Equal("screens", error.Errors.Single().Field);
    }

    [Fact]
    public async Task User_list_searches_and_reports_access_type()
    {
        var kit = new IdentityTestKit().As(IdentityTestKit.Admin);
        await kit.Access.SetUserAccessAsync(kit.UserId(IdentityTestKit.Learner), new SetUserAccessRequest(["users"]), Ct);

        var all = await kit.As(IdentityTestKit.Admin).Access.GetUsersAsync(new UserListQuery { PageSize = 2 }, Ct);
        var found = await kit.Access.GetUsersAsync(new UserListQuery { Search = "learner" }, Ct);

        Assert.Equal(5, all.Total);
        Assert.Equal(2, all.Items.Count);
        Assert.Equal(AccessTypes.Full, all.Items[0].AccessType);
        var learner = Assert.Single(found.Items);
        Assert.Equal(AccessTypes.Custom, learner.AccessType);
        Assert.Equal(["users"], learner.CustomScreens);
    }

    private sealed class GrantComparer : IEqualityComparer<CourseGrantDto>
    {
        public bool Equals(CourseGrantDto? x, CourseGrantDto? y) =>
            x is not null && y is not null && x.CourseId == y.CourseId && x.Permissions.SequenceEqual(y.Permissions);

        public int GetHashCode(CourseGrantDto obj) => obj.CourseId;
    }
}
