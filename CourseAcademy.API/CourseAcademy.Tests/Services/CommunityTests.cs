using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

/// <summary>Course communities: posts, comments, replies and moderation (BR-COM-01 to 16).</summary>
public class CommunityTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly PageQuery FirstPage = new();

    /// <summary>A free published course; the demo student is enrolled and signed in. The learner is not enrolled.</summary>
    private static async Task<(CatalogTests.Kit Kit, CourseDetailDto Course)> SetUp()
    {
        var kit = new CatalogTests.Kit().As(IdentityTestKit.Admin);
        var created = await kit.Admin.CreateAsync(kit.Course("free-course") with { OriginalPrice = 0, Price = 0 }, Ct);
        await kit.Admin.SaveCurriculumAsync(created.Id, CatalogTests.TwoSections(), Ct);
        var course = await kit.Admin.SetPublishedAsync(created.Id, true, Ct);
        kit.As(IdentityTestKit.Student);
        await kit.Enrollments.EnrollFreeAsync(course.Slug, Ct);
        return (kit, course);
    }

    [Fact]
    public async Task Members_post_and_comment_while_visitors_and_outsiders_only_see_the_count()
    {
        var (kit, course) = await SetUp();
        var post = await kit.Community.CreatePostAsync(course.Slug, new("  How do I start?  ", " Any tips? "), Ct);
        Assert.Equal(("How do I start?", "Any tips?", "student", true, true), (post.Title, post.Body, post.Author.Badge, post.CanEdit, post.CanComment));

        Assert.Equal("Give your post a title.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Community.CreatePostAsync(course.Slug, new(" ", "Body"), Ct))).Message);
        Assert.Equal("This is too long. Please shorten it.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Community.AddCommentAsync(post.Id, new(new string('c', 1001), null), Ct))).Message);

        kit.Identity.CurrentUser.Set(null);
        Assert.Equal(new CommunityInfoDto(false, false, false, 1), await kit.Community.GetInfoAsync(course.Slug, Ct));

        // Not enrolled: no access (BR-COM-02).
        kit.As(IdentityTestKit.Learner);
        Assert.False((await kit.Community.GetInfoAsync(course.Slug, Ct)).CanRead);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Community.AddCommentAsync(post.Id, new("Hi", null), Ct));
    }

    [Fact]
    public async Task Replies_are_one_level_deep_and_locked_posts_take_comments_from_moderators_only()
    {
        var (kit, course) = await SetUp();
        var post = await kit.Community.CreatePostAsync(course.Slug, new("Question", "Body"), Ct);
        var top = await kit.Community.AddCommentAsync(post.Id, new("First", null), Ct);
        var reply = await kit.Community.AddCommentAsync(post.Id, new("Reply", top.Id), Ct);
        var replyToReply = await kit.Community.AddCommentAsync(post.Id, new("Reply to reply", reply.Id), Ct);
        Assert.Equal(top.Id, replyToReply.ParentId);
        Assert.Equal("The comment you replied to was removed.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Community.AddCommentAsync(post.Id, new("x", 999_999), Ct))).Message);

        var thread = Assert.Single(await kit.Community.GetCommentsAsync(post.Id, Ct));
        Assert.Equal(["Reply", "Reply to reply"], thread.Replies.Select(r => r.Body));
        Assert.Equal(3, (await kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct)).Items[0].CommentCount);

        kit.As(IdentityTestKit.Admin);
        var locked = await kit.Community.SetPostFlagsAsync(post.Id, new(true, true, null), Ct);
        Assert.Equal((true, true, false), (locked.IsPinned, locked.IsLocked, locked.IsHidden));
        Assert.Equal("admin", (await kit.Community.AddCommentAsync(post.Id, new("Closing this.", null), Ct)).Author.Badge);

        kit.As(IdentityTestKit.Student);
        Assert.False((await kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct)).Items[0].CanComment);
        var refused = await Assert.ThrowsAsync<ConflictException>(() => kit.Community.AddCommentAsync(post.Id, new("More?", null), Ct));
        Assert.Equal("This discussion is locked.", refused.Message);
    }

    [Fact]
    public async Task Hidden_items_are_shown_to_moderators_only_and_counts_follow_what_the_viewer_sees()
    {
        var (kit, course) = await SetUp();
        var visible = await kit.Community.CreatePostAsync(course.Slug, new("Visible", "Body"), Ct);
        var spam = await kit.Community.CreatePostAsync(course.Slug, new("Spam", "Buy now"), Ct);
        var top = await kit.Community.AddCommentAsync(visible.Id, new("Rude", null), Ct);
        await kit.Community.AddCommentAsync(visible.Id, new("Reply under rude", top.Id), Ct);
        await kit.Community.AddCommentAsync(visible.Id, new("Kind", null), Ct);

        kit.As(IdentityTestKit.Admin);
        await kit.Community.SetPostFlagsAsync(spam.Id, new(null, null, true), Ct);
        await kit.Community.SetCommentHiddenAsync(top.Id, true, Ct);
        var forModerator = await kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct);
        Assert.Equal(2, forModerator.Total);
        Assert.Equal(3, forModerator.Items.Single(p => p.Id == visible.Id).CommentCount);

        kit.As(IdentityTestKit.Student);
        var forStudent = await kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct);
        var only = Assert.Single(forStudent.Items);
        // The hidden comment and the reply under it are left out (BR-COM-06).
        Assert.Equal(1, only.CommentCount);
        Assert.Equal(["Kind"], (await kit.Community.GetCommentsAsync(visible.Id, Ct)).Select(c => c.Body));
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Community.GetCommentsAsync(spam.Id, Ct));
        Assert.Equal(1, (await kit.Community.GetInfoAsync(course.Slug, Ct)).PublicPostCount);
    }

    [Fact]
    public async Task Only_authors_edit_and_authors_or_moderators_delete_with_the_comments()
    {
        var (kit, course) = await SetUp();
        var post = await kit.Community.CreatePostAsync(course.Slug, new("Title", "Body"), Ct);
        var comment = await kit.Community.AddCommentAsync(post.Id, new("Comment", null), Ct);
        await kit.Community.AddCommentAsync(post.Id, new("Reply", comment.Id), Ct);
        var edited = await kit.Community.UpdatePostAsync(post.Id, new("New title", "New body"), Ct);
        Assert.Equal(("New title", TestDb.Start.UtcDateTime), (edited.Title, edited.EditedAt));

        // Moderators cannot edit other people's posts (BR-COM-08), but they can delete them (BR-COM-09).
        kit.As(IdentityTestKit.Admin);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Community.UpdatePostAsync(post.Id, new("Mine now", "x"), Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Community.UpdateCommentAsync(comment.Id, new("x"), Ct));
        await kit.Community.DeletePostAsync(post.Id, Ct);

        Assert.Empty(await kit.Identity.Db.Comments.ToListAsync());
        Assert.Equal(2, await kit.Identity.Db.Comments.IgnoreQueryFilters().CountAsync(c => c.IsDeleted));
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Community.GetCommentsAsync(post.Id, Ct));
    }

    [Fact]
    public async Task Badges_show_instructors_and_former_members_and_moderators_list_only_their_courses()
    {
        var (kit, course) = await SetUp();
        await kit.Community.CreatePostAsync(course.Slug, new("From a student", "Body"), Ct);

        kit.As(IdentityTestKit.Admin);
        var instructorId = kit.Identity.UserId(IdentityTestKit.Instructor);
        await kit.Identity.Access.SetCourseGrantAsync(instructorId, course.Id, new SetCourseGrantRequest(["courses.view", "courses.edit", "community.moderate"]), Ct);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            kit.As(IdentityTestKit.Student).Community.GetModerationPostsAsync(new ModerationPostQuery(), Ct));

        kit.As(IdentityTestKit.Instructor);
        var instructorPost = await kit.Community.CreatePostAsync(course.Slug, new("Welcome", "Hello"), Ct);
        Assert.Equal("instructor", instructorPost.Author.Badge);
        var list = await kit.Community.GetModerationPostsAsync(new ModerationPostQuery { Search = "student" }, Ct);
        var row = Assert.Single(list.Items);
        Assert.Equal(("From a student", course.Slug, true), (row.Post.Title, row.CourseSlug, row.Post.CanDelete));
        Assert.Empty(await kit.Community.GetModerationCommentsAsync(new ModerationCommentQuery { Status = "all" }, Ct));

        // A deleted account shows as "Former member" (BR-COM-13).
        var student = await kit.Identity.Db.Users.SingleAsync(u => u.Email == IdentityTestKit.Student);
        student.IsDeleted = true;
        await kit.Identity.Db.SaveChangesAsync();
        kit.Identity.Db.ChangeTracker.Clear();
        var posts = await kit.Community.GetPostsAsync(course.Slug, FirstPage, Ct);
        Assert.Equal(new AuthorDto(null, "Former member", "student"), posts.Items.Single(p => p.Title == "From a student").Author);
    }

    [Fact]
    public async Task Deleting_a_course_removes_its_community_but_its_certificates_still_verify()
    {
        var (kit, course) = await SetUp();
        var post = await kit.Community.CreatePostAsync(course.Slug, new("Title", "Body"), Ct);
        await kit.Community.AddCommentAsync(post.Id, new("Comment", null), Ct);
        foreach (var section in course.Modules[0].Submodules[0].Sections) await kit.Progress.SetSectionCompletedAsync(section.Id, true, Ct);
        var certificate = Assert.Single(await kit.Certification.GetMineAsync(Ct));

        kit.As(IdentityTestKit.Admin);
        await kit.Admin.DeleteAsync(course.Id, Ct);

        Assert.Empty(await kit.Identity.Db.Posts.ToListAsync());
        Assert.Empty(await kit.Identity.Db.Comments.ToListAsync());
        var verified = await kit.Certification.VerifyAsync(certificate.Code, Ct);
        Assert.Equal(("Photography Basics", false), (verified.CourseTitle, verified.IsRevoked));
        Assert.Contains(await kit.Identity.Db.Set<Post>().IgnoreQueryFilters().ToListAsync(), p => p.IsDeleted);
    }
}
