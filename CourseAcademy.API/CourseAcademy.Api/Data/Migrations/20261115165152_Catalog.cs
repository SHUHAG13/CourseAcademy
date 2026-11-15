using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategorySkill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySkill_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Instructor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: false),
                    Students = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructor", x => x.Id);
                    table.CheckConstraint("CK_Instructor_ExperienceYears", "[ExperienceYears] >= 0");
                    table.CheckConstraint("CK_Instructor_Rating", "[Rating] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_Instructor_Students", "[Students] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CourseFormatId = table.Column<int>(type: "int", nullable: false),
                    CourseLevelId = table.Column<int>(type: "int", nullable: false),
                    CourseBadgeId = table.Column<int>(type: "int", nullable: true),
                    InstructorId = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CoverLabel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CoverIcon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    Students = table.Column<int>(type: "int", nullable: false),
                    ContentUpdatedMonth = table.Column<string>(type: "char(7)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Course", x => x.Id);
                    table.CheckConstraint("CK_Course_Counts", "[RatingCount] >= 0 AND [Students] >= 0");
                    table.CheckConstraint("CK_Course_DurationHours", "[DurationHours] >= 1");
                    table.CheckConstraint("CK_Course_Prices", "[Price] >= 0 AND [OriginalPrice] >= 0 AND [Price] <= [OriginalPrice]");
                    table.CheckConstraint("CK_Course_Rating", "[Rating] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_Course_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseBadge_CourseBadgeId",
                        column: x => x.CourseBadgeId,
                        principalTable: "CourseBadge",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseFormat_CourseFormatId",
                        column: x => x.CourseFormatId,
                        principalTable: "CourseFormat",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseLevel_CourseLevelId",
                        column: x => x.CourseLevelId,
                        principalTable: "CourseLevel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_Instructor_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InstructorExpertise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstructorId = table.Column<int>(type: "int", nullable: false),
                    Expertise = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorExpertise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorExpertise_Instructor_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseModule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseModule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseModule_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseOutcome",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOutcome", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOutcome_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseRequirement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequirement_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LiveSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MeetingUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSession", x => x.Id);
                    table.CheckConstraint("CK_LiveSession_Duration", "[DurationMinutes] >= 1");
                    table.ForeignKey(
                        name: "FK_LiveSession_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    MaterialTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Material_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Material_MaterialType_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalTable: "MaterialType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Submodule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseModuleId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submodule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submodule_CourseModule_CourseModuleId",
                        column: x => x.CourseModuleId,
                        principalTable: "CourseModule",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Section",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SubmoduleId = table.Column<int>(type: "int", nullable: true),
                    ParentSectionId = table.Column<int>(type: "int", nullable: true),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Section", x => x.Id);
                    table.CheckConstraint("CK_Section_Parent", "([SubmoduleId] IS NOT NULL AND [ParentSectionId] IS NULL AND [Depth] = 0) OR ([SubmoduleId] IS NULL AND [ParentSectionId] IS NOT NULL AND [Depth] BETWEEN 1 AND 3)");
                    table.ForeignKey(
                        name: "FK_Section_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Section_Section_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalTable: "Section",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Section_Submodule_SubmoduleId",
                        column: x => x.SubmoduleId,
                        principalTable: "Submodule",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    IsPreview = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionVideo", x => x.Id);
                    table.CheckConstraint("CK_SectionVideo_Minutes", "[Minutes] >= 1");
                    table.ForeignKey(
                        name: "FK_SectionVideo_Section_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Section",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SectionVideoId = table.Column<int>(type: "int", nullable: true),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionDocument_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionDocument_SectionVideo_SectionVideoId",
                        column: x => x.SectionVideoId,
                        principalTable: "SectionVideo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionDocument_Section_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Section",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "CategorySkill",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Skill", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Premiere Pro", 1, null },
                    { 2, 1, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DaVinci Resolve", 2, null },
                    { 3, 1, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "After Effects", 3, null },
                    { 4, 2, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SEO", 1, null },
                    { 5, 2, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Meta Ads", 2, null },
                    { 6, 2, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Analytics", 3, null },
                    { 7, 3, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Match reporting", 1, null },
                    { 8, 3, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Interviewing", 2, null },
                    { 9, 3, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sports video", 3, null },
                    { 10, 4, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TypeScript", 1, null },
                    { 11, 4, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Angular", 2, null },
                    { 12, 4, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ASP.NET Core", 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_InstructorId",
                table: "User",
                column: "InstructorId",
                unique: true,
                filter: "[InstructorId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySkill_CategoryId_SortOrder",
                table: "CategorySkill",
                columns: new[] { "CategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Course_CategoryId",
                table: "Course",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseBadgeId",
                table: "Course",
                column: "CourseBadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseFormatId",
                table: "Course",
                column: "CourseFormatId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseLevelId",
                table: "Course",
                column: "CourseLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_InstructorId",
                table: "Course",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_IsFeatured",
                table: "Course",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Course_IsPublished_CategoryId",
                table: "Course",
                columns: new[] { "IsPublished", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Course_Slug",
                table: "Course",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseModule_CourseId_SortOrder",
                table: "CourseModule",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOutcome_CourseId_SortOrder",
                table: "CourseOutcome",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequirement_CourseId_SortOrder",
                table: "CourseRequirement",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorExpertise_InstructorId_SortOrder",
                table: "InstructorExpertise",
                columns: new[] { "InstructorId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSession_CourseId_StartsAtUtc",
                table: "LiveSession",
                columns: new[] { "CourseId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Material_AssetId",
                table: "Material",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Material_CourseId_SortOrder",
                table: "Material",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Material_MaterialTypeId",
                table: "Material",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Section_CourseId",
                table: "Section",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Section_ParentSectionId_SortOrder",
                table: "Section",
                columns: new[] { "ParentSectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Section_SubmoduleId_SortOrder",
                table: "Section",
                columns: new[] { "SubmoduleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_AssetId",
                table: "SectionDocument",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_DocumentTypeId",
                table: "SectionDocument",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_SectionId_SortOrder",
                table: "SectionDocument",
                columns: new[] { "SectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_SectionVideoId",
                table: "SectionDocument",
                column: "SectionVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_AssetId",
                table: "SectionVideo",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_SectionId_SortOrder",
                table: "SectionVideo",
                columns: new[] { "SectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Submodule_CourseModuleId_SortOrder",
                table: "Submodule",
                columns: new[] { "CourseModuleId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseGrant_Course_CourseId",
                table: "CourseGrant",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Instructor_InstructorId",
                table: "User",
                column: "InstructorId",
                principalTable: "Instructor",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseGrant_Course_CourseId",
                table: "CourseGrant");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Instructor_InstructorId",
                table: "User");

            migrationBuilder.DropTable(
                name: "CategorySkill");

            migrationBuilder.DropTable(
                name: "CourseOutcome");

            migrationBuilder.DropTable(
                name: "CourseRequirement");

            migrationBuilder.DropTable(
                name: "InstructorExpertise");

            migrationBuilder.DropTable(
                name: "LiveSession");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "SectionDocument");

            migrationBuilder.DropTable(
                name: "SectionVideo");

            migrationBuilder.DropTable(
                name: "Section");

            migrationBuilder.DropTable(
                name: "Submodule");

            migrationBuilder.DropTable(
                name: "CourseModule");

            migrationBuilder.DropTable(
                name: "Course");

            migrationBuilder.DropTable(
                name: "Instructor");

            migrationBuilder.DropIndex(
                name: "IX_User_InstructorId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "User");
        }
    }
}
