using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Api.Services;

/// <summary>Creating, editing and deleting instructor profiles (BR-CMS-14, 15). Reading is public, see <see cref="ICatalogService"/>.</summary>
public interface IInstructorService
{
    Task<InstructorDto> CreateAsync(InstructorSaveRequest request, CancellationToken ct);
    Task<InstructorDto> UpdateAsync(int id, InstructorSaveRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class InstructorService(
    IInstructorRepository instructors,
    IRepository<InstructorExpertise> expertise,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IInstructorService
{
    public async Task<InstructorDto> CreateAsync(InstructorSaveRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.InstructorsCreate, null, ct);
        var instructor = new Instructor();
        Apply(instructor, request);
        await instructors.AddAsync(instructor, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CourseMapper.ToDto(instructor);
    }

    public async Task<InstructorDto> UpdateAsync(int id, InstructorSaveRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.InstructorsEdit, null, ct);
        var instructor = await instructors.GetWithExpertiseAsync(id, ct) ?? throw new NotFoundException("Instructor not found.");
        foreach (var old in instructor.Expertise.ToList()) expertise.Remove(old);
        instructor.Expertise.Clear();
        Apply(instructor, request);
        await unitOfWork.SaveChangesAsync(ct);
        return CourseMapper.ToDto(instructor);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.InstructorsDelete, null, ct);
        var instructor = await instructors.GetByIdAsync(id, ct) ?? throw new NotFoundException("Instructor not found.");
        var assigned = await instructors.CountCoursesAsync(id, ct);
        if (assigned > 0)
        {
            throw new ConflictException($"{instructor.Name} teaches {assigned} course(s). Reassign them before deleting.");
        }

        // An account that taught as this instructor no longer does.
        foreach (var user in await users.ListAsync(u => u.InstructorId == id, ct))
        {
            var tracked = await users.GetByIdAsync(user.Id, ct);
            if (tracked is not null) tracked.InstructorId = null;
        }
        instructors.Remove(instructor);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static void Apply(Instructor instructor, InstructorSaveRequest r)
    {
        instructor.Name = r.Name.Trim();
        instructor.Title = r.Title.Trim();
        instructor.Bio = r.Bio.Trim();
        instructor.ExperienceYears = r.ExperienceYears;
        instructor.Students = r.Students;
        var order = 0;
        foreach (var skill in (r.Expertise ?? []).Select(e => e.Trim()).Where(e => e.Length > 0).Distinct())
        {
            instructor.Expertise.Add(new InstructorExpertise { Expertise = skill, SortOrder = ++order });
        }
    }
}
