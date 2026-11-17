using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lookups");

            migrationBuilder.EnsureSchema(
                name: "Catalog");

            migrationBuilder.EnsureSchema(
                name: "Securities");

            migrationBuilder.EnsureSchema(
                name: "Commerce");

            migrationBuilder.EnsureSchema(
                name: "Learning");

            migrationBuilder.EnsureSchema(
                name: "Media");

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseBadge",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBadge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseFormat",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFormat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseLevel",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLevel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentType",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentStatus",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instructor",
                schema: "Catalog",
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
                name: "MaterialType",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssetStatus",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssetStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaKind",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaKind", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatus",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Screen",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCourseScoped = table.Column<bool>(type: "bit", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Screen", x => x.Id);
                    table.CheckConstraint("CK_Screen_Area", "[Area] IN ('admin', 'student')");
                });

            migrationBuilder.CreateTable(
                name: "StreamingWebhookEvent",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingWebhookEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategorySkill",
                schema: "Lookups",
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
                        principalSchema: "Lookups",
                        principalTable: "Category",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Course",
                schema: "Catalog",
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
                        principalSchema: "Lookups",
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseBadge_CourseBadgeId",
                        column: x => x.CourseBadgeId,
                        principalSchema: "Lookups",
                        principalTable: "CourseBadge",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseFormat_CourseFormatId",
                        column: x => x.CourseFormatId,
                        principalSchema: "Lookups",
                        principalTable: "CourseFormat",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_CourseLevel_CourseLevelId",
                        column: x => x.CourseLevelId,
                        principalSchema: "Lookups",
                        principalTable: "CourseLevel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Course_Instructor_InstructorId",
                        column: x => x.InstructorId,
                        principalSchema: "Catalog",
                        principalTable: "Instructor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InstructorExpertise",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Instructor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    HasCustomAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InstructorId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Instructor_InstructorId",
                        column: x => x.InstructorId,
                        principalSchema: "Catalog",
                        principalTable: "Instructor",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_User_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Securities",
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScreenId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsCourseGrantable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_Screen_ScreenId",
                        column: x => x.ScreenId,
                        principalSchema: "Securities",
                        principalTable: "Screen",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleScreen",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ScreenId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleScreen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleScreen_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Securities",
                        principalTable: "Role",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoleScreen_Screen_ScreenId",
                        column: x => x.ScreenId,
                        principalSchema: "Securities",
                        principalTable: "Screen",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseModule",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseOutcome",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseRequirement",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LiveSession",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseGrant",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGrant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGrant_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseGrant_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaAsset",
                schema: "Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MediaKindId = table.Column<int>(type: "int", nullable: false),
                    MediaAssetStatusId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
                    UploadExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DeleteRequestedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAsset", x => x.Id);
                    table.CheckConstraint("CK_MediaAsset_Numbers", "[SizeBytes] > 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] >= 0) AND [LastSequence] >= 0");
                    table.ForeignKey(
                        name: "FK_MediaAsset_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_MediaAssetStatus_MediaAssetStatusId",
                        column: x => x.MediaAssetStatusId,
                        principalSchema: "Lookups",
                        principalTable: "MediaAssetStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_MediaKind_MediaKindId",
                        column: x => x.MediaKindId,
                        principalSchema: "Lookups",
                        principalTable: "MediaKind",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_User_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                schema: "Commerce",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Gateway = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GatewayReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    MaskedAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    PaymentStatusId = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.Id);
                    table.CheckConstraint("CK_PaymentTransaction_Amounts", "[OriginalAmount] >= 0 AND [DiscountAmount] >= 0 AND [Amount] >= 0 AND [Amount] + [DiscountAmount] = [OriginalAmount]");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Lookups",
                        principalTable: "Currency",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_PaymentMethod_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "Lookups",
                        principalTable: "PaymentMethod",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_PaymentStatus_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalSchema: "Lookups",
                        principalTable: "PaymentStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReplacedByTokenId = table.Column<int>(type: "int", nullable: true),
                    IsPersistent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_RefreshToken_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalSchema: "Securities",
                        principalTable: "RefreshToken",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshToken_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserScreen",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ScreenId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScreen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserScreen_Screen_ScreenId",
                        column: x => x.ScreenId,
                        principalSchema: "Securities",
                        principalTable: "Screen",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserScreen_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Submodule",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "CourseModule",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseGrantPermission",
                schema: "Securities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseGrantId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGrantPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGrantPermission_CourseGrant_CourseGrantId",
                        column: x => x.CourseGrantId,
                        principalSchema: "Securities",
                        principalTable: "CourseGrant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseGrantPermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Securities",
                        principalTable: "Permission",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Material",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    MaterialTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MediaAssetId = table.Column<int>(type: "int", nullable: true),
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Material_MaterialType_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalSchema: "Lookups",
                        principalTable: "MaterialType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Material_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "Media",
                        principalTable: "MediaAsset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollment",
                schema: "Commerce",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentStatusId = table.Column<int>(type: "int", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PaymentTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollment_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_EnrollmentStatus_EnrollmentStatusId",
                        column: x => x.EnrollmentStatusId,
                        principalSchema: "Lookups",
                        principalTable: "EnrollmentStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_PaymentTransaction_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "Commerce",
                        principalTable: "PaymentTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Section",
                schema: "Catalog",
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
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Section_Section_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalSchema: "Catalog",
                        principalTable: "Section",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Section_Submodule_SubmoduleId",
                        column: x => x.SubmoduleId,
                        principalSchema: "Catalog",
                        principalTable: "Submodule",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionVideo",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MediaAssetId = table.Column<int>(type: "int", nullable: true),
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
                        name: "FK_SectionVideo_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "Media",
                        principalTable: "MediaAsset",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionVideo_Section_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "Catalog",
                        principalTable: "Section",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LessonProgress",
                schema: "Learning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SectionVideoId = table.Column<int>(type: "int", nullable: true),
                    PositionSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxWatchedSeconds = table.Column<int>(type: "int", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgress", x => x.Id);
                    table.CheckConstraint("CK_LessonProgress_Seconds", "[PositionSeconds] >= 0 AND [MaxWatchedSeconds] >= 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] > 0)");
                    table.ForeignKey(
                        name: "FK_LessonProgress_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_SectionVideo_SectionVideoId",
                        column: x => x.SectionVideoId,
                        principalSchema: "Catalog",
                        principalTable: "SectionVideo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_Section_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "Catalog",
                        principalTable: "Section",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionDocument",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SectionVideoId = table.Column<int>(type: "int", nullable: true),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MediaAssetId = table.Column<int>(type: "int", nullable: true),
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
                        principalSchema: "Lookups",
                        principalTable: "DocumentType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionDocument_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "Media",
                        principalTable: "MediaAsset",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionDocument_SectionVideo_SectionVideoId",
                        column: x => x.SectionVideoId,
                        principalSchema: "Catalog",
                        principalTable: "SectionVideo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SectionDocument_Section_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "Catalog",
                        principalTable: "Section",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "Category",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Icon", "ImageUrl", "Name", "ShortName", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "video-editing", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edit, colour-grade and publish professional videos for YouTube, brands and broadcast.", "bi-camera-reels", "/images/web/creative-studio.webp", "Video Editing", "Video Editing", 1, null },
                    { 2, "digital-marketing", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan campaigns, run paid ads and measure the growth they bring to a real business.", "bi-megaphone", "/images/web/contact-bg.webp", "Digital Marketing", "Marketing", 2, null },
                    { 3, "sports-journalism", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Report, write and present sport like a professional — from match reports to live coverage.", "bi-mic", "/images/web/pitchside-camera.webp", "Sports Journalism", "Sports Journalism", 3, null },
                    { 4, "software-development", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Build real websites and apps with modern tools used by software teams today.", "bi-code-slash", "/images/web/dev-team.webp", "Software Development", "Development", 4, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "CourseBadge",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Bestseller", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bestseller", 1, null },
                    { 2, "New", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New", 2, null },
                    { 3, "Popular", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Popular", 3, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "CourseFormat",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Pre-recorded", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pre-recorded", 1, null },
                    { 2, "Live", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Live", 2, null },
                    { 3, "Hybrid", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hybrid", 3, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "CourseLevel",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Beginner", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Beginner", 1, null },
                    { 2, "Intermediate", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Intermediate", 2, null },
                    { 3, "Advanced", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced", 3, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "Currency",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[] { 1, "BDT", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bangladeshi Taka", 1, null });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "DocumentType",
                columns: new[] { "Id", "Code", "CreatedAt", "FileExtension", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "PDF", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".pdf", "PDF", 1, null },
                    { 2, "DOCX", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".docx", "Word document", 2, null },
                    { 3, "PPTX", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".pptx", "PowerPoint slides", 3, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "EnrollmentStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "active", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active", 1, null },
                    { 2, "revoked", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Revoked", 2, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "MaterialType",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "PDF", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PDF", 1, null },
                    { 2, "Slides", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Slides", 2, null },
                    { 3, "Worksheet", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Worksheet", 3, null },
                    { 4, "Link", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Link", 4, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "MediaAssetStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "pending_upload", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Waiting for upload", 1, null },
                    { 2, "uploaded", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Uploaded", 2, null },
                    { 3, "queued", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Queued", 3, null },
                    { 4, "processing", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Processing", 4, null },
                    { 5, "ready", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ready", 5, null },
                    { 6, "failed", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed", 6, null },
                    { 7, "deleting", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deleting", 7, null },
                    { 8, "deleted", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deleted", 8, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "MediaKind",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "video", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Video", 1, null },
                    { 2, "document", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Document", 2, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "PaymentMethod",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Icon", "LogoUrl", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "bkash", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pay from your bKash wallet.", "bi-wallet2", "/logos/bkash.jpg", "bKash", 1, null },
                    { 2, "nagad", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pay from your Nagad wallet.", "bi-wallet2", "/logos/nagad.png", "Nagad", 2, null },
                    { 3, "rocket", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pay from your Rocket account.", "bi-rocket-takeoff", null, "Rocket", 3, null },
                    { 4, "card", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pay by debit or credit card.", "bi-credit-card-2-front", null, "Visa / Mastercard", 4, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "PaymentStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "pending", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pending", 1, null },
                    { 2, "success", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Success", 2, null },
                    { 3, "failed", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Failed", 3, null },
                    { 4, "cancelled", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cancelled", 4, null },
                    { 5, "refunded", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refunded", 5, null }
                });

            migrationBuilder.InsertData(
                schema: "Securities",
                table: "Role",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsSystem", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "admin", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full access to every part of the academy.", true, "Admin", null },
                    { 2, "instructor", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Opens the admin panel for the courses they are granted.", true, "Instructor", null },
                    { 3, "user", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Learns in the courses they are enrolled in.", true, "Student", null }
                });

            migrationBuilder.InsertData(
                schema: "Securities",
                table: "Screen",
                columns: new[] { "Id", "Area", "Code", "CreatedAt", "Icon", "IsCourseScoped", "Name", "Path", "Section", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "admin", "overview", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-speedometer2", false, "Overview", "/admin", null, 1, null },
                    { 2, "admin", "courses", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-journal-text", true, "Courses", "/admin/courses", null, 2, null },
                    { 3, "admin", "instructors", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-person-video3", false, "Instructors", "/admin/instructors", null, 3, null },
                    { 4, "admin", "users", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-people", false, "Users", "/admin/users", null, 4, null },
                    { 5, "admin", "enrollments", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-card-checklist", false, "Enrollments", "/admin/enrollments", null, 5, null },
                    { 6, "admin", "transactions", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-credit-card", false, "Transactions", "/admin/transactions", null, 6, null },
                    { 7, "admin", "roles", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-person-badge", false, "Roles & permissions", "/admin/security/roles", "security", 7, null },
                    { 8, "admin", "access", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-person-lock", false, "User access", "/admin/security/access", "security", 8, null },
                    { 9, "admin", "community-moderation", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-chat-square-text", true, "Community", "/admin/community", null, 9, null },
                    { 10, "admin", "certificates", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-award", true, "Certificates", "/admin/certificates", null, 10, null },
                    { 11, "student", "dashboard", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-grid", false, "My dashboard", "/dashboard", null, 11, null },
                    { 12, "student", "learn", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-play-circle", false, "Learning area", "/dashboard", null, 12, null },
                    { 13, "student", "checkout", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-bag", false, "Checkout", "/courses", null, 13, null },
                    { 14, "student", "community", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-chat-dots", false, "Course community", "/dashboard", null, 14, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
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

            migrationBuilder.InsertData(
                schema: "Securities",
                table: "Permission",
                columns: new[] { "Id", "Action", "Code", "CreatedAt", "IsCourseGrantable", "Name", "ScreenId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "view", "reports.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View the overview and reports", 1, 1, null },
                    { 2, "extra", "system.reset", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Reset demo data", 1, 2, null },
                    { 3, "view", "courses.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "View courses, including drafts", 2, 3, null },
                    { 4, "create", "courses.create", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Create courses", 2, 4, null },
                    { 5, "edit", "courses.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Edit and publish courses", 2, 5, null },
                    { 6, "delete", "courses.delete", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Delete courses", 2, 6, null },
                    { 7, "view", "instructors.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View instructors", 3, 7, null },
                    { 8, "create", "instructors.create", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Create instructors", 3, 8, null },
                    { 9, "edit", "instructors.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Edit instructors", 3, 9, null },
                    { 10, "delete", "instructors.delete", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Delete instructors", 3, 10, null },
                    { 11, "view", "users.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View users", 4, 11, null },
                    { 12, "view", "enrollments.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View enrollments", 5, 12, null },
                    { 13, "edit", "enrollments.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Change enrollments", 5, 13, null },
                    { 14, "view", "transactions.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View transactions", 6, 14, null },
                    { 15, "edit", "transactions.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Change transactions", 6, 15, null },
                    { 16, "delete", "transactions.delete", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Delete transactions", 6, 16, null },
                    { 17, "view", "roles.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View roles", 7, 17, null },
                    { 18, "edit", "roles.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Create, edit and assign roles", 7, 18, null },
                    { 19, "view", "access.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "View user access", 8, 19, null },
                    { 20, "edit", "access.edit", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Change user access", 8, 20, null },
                    { 21, "view", "community.moderate", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Moderate course communities", 9, 21, null },
                    { 22, "view", "certificates.manage", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Set certification rules, review exams and manage certificates", 10, 22, null },
                    { 23, "view", "dashboard.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Open the student dashboard", 11, 23, null },
                    { 24, "view", "learn.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Open enrolled courses", 12, 24, null },
                    { 25, "view", "checkout.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Buy courses", 13, 25, null },
                    { 26, "view", "community.view", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Read course communities", 14, 26, null },
                    { 27, "create", "community.post", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Write posts, comments and replies", 14, 27, null }
                });

            migrationBuilder.InsertData(
                schema: "Securities",
                table: "RoleScreen",
                columns: new[] { "Id", "CreatedAt", "RoleId", "ScreenId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 11, null },
                    { 2, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 12, null },
                    { 3, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 13, null },
                    { 4, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 14, null },
                    { 5, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 11, null },
                    { 6, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 12, null },
                    { 7, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 13, null },
                    { 8, new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 14, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Category_Code",
                schema: "Lookups",
                table: "Category",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySkill_CategoryId_SortOrder",
                schema: "Lookups",
                table: "CategorySkill",
                columns: new[] { "CategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Course_CategoryId",
                schema: "Catalog",
                table: "Course",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseBadgeId",
                schema: "Catalog",
                table: "Course",
                column: "CourseBadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseFormatId",
                schema: "Catalog",
                table: "Course",
                column: "CourseFormatId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CourseLevelId",
                schema: "Catalog",
                table: "Course",
                column: "CourseLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_InstructorId",
                schema: "Catalog",
                table: "Course",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_IsFeatured",
                schema: "Catalog",
                table: "Course",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Course_IsPublished_CategoryId",
                schema: "Catalog",
                table: "Course",
                columns: new[] { "IsPublished", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Course_Slug",
                schema: "Catalog",
                table: "Course",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBadge_Code",
                schema: "Lookups",
                table: "CourseBadge",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFormat_Code",
                schema: "Lookups",
                table: "CourseFormat",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrant_CourseId",
                schema: "Securities",
                table: "CourseGrant",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrant_UserId_CourseId",
                schema: "Securities",
                table: "CourseGrant",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrantPermission_CourseGrantId_PermissionId",
                schema: "Securities",
                table: "CourseGrantPermission",
                columns: new[] { "CourseGrantId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrantPermission_PermissionId",
                schema: "Securities",
                table: "CourseGrantPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLevel_Code",
                schema: "Lookups",
                table: "CourseLevel",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseModule_CourseId_SortOrder",
                schema: "Catalog",
                table: "CourseModule",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOutcome_CourseId_SortOrder",
                schema: "Catalog",
                table: "CourseOutcome",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequirement_CourseId_SortOrder",
                schema: "Catalog",
                table: "CourseRequirement",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Currency_Code",
                schema: "Lookups",
                table: "Currency",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_Code",
                schema: "Lookups",
                table: "DocumentType",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_CourseId_EnrollmentStatusId",
                schema: "Commerce",
                table: "Enrollment",
                columns: new[] { "CourseId", "EnrollmentStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_EnrollmentStatusId",
                schema: "Commerce",
                table: "Enrollment",
                column: "EnrollmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_PaymentTransactionId",
                schema: "Commerce",
                table: "Enrollment",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_UserId_CourseId",
                schema: "Commerce",
                table: "Enrollment",
                columns: new[] { "UserId", "CourseId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatus_Code",
                schema: "Lookups",
                table: "EnrollmentStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorExpertise_InstructorId_SortOrder",
                schema: "Catalog",
                table: "InstructorExpertise",
                columns: new[] { "InstructorId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_CourseId",
                schema: "Learning",
                table: "LessonProgress",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_SectionId",
                schema: "Learning",
                table: "LessonProgress",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_SectionVideoId",
                schema: "Learning",
                table: "LessonProgress",
                column: "SectionVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_CourseId",
                schema: "Learning",
                table: "LessonProgress",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_SectionId_SectionVideoId",
                schema: "Learning",
                table: "LessonProgress",
                columns: new[] { "UserId", "SectionId", "SectionVideoId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSession_CourseId_StartsAtUtc",
                schema: "Catalog",
                table: "LiveSession",
                columns: new[] { "CourseId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Material_CourseId_SortOrder",
                schema: "Catalog",
                table: "Material",
                columns: new[] { "CourseId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Material_MaterialTypeId",
                schema: "Catalog",
                table: "Material",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_MediaAssetId",
                schema: "Catalog",
                table: "Material",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialType_Code",
                schema: "Lookups",
                table: "MaterialType",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_AssetId",
                schema: "Media",
                table: "MediaAsset",
                column: "AssetId",
                unique: true,
                filter: "[AssetId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_CourseId",
                schema: "Media",
                table: "MediaAsset",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_MediaAssetStatusId_DeleteRequestedAt",
                schema: "Media",
                table: "MediaAsset",
                columns: new[] { "MediaAssetStatusId", "DeleteRequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_MediaKindId",
                schema: "Media",
                table: "MediaAsset",
                column: "MediaKindId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_UploadedByUserId",
                schema: "Media",
                table: "MediaAsset",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetStatus_Code",
                schema: "Lookups",
                table: "MediaAssetStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaKind_Code",
                schema: "Lookups",
                table: "MediaKind",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Code",
                schema: "Lookups",
                table: "PaymentMethod",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatus_Code",
                schema: "Lookups",
                table: "PaymentStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CourseId",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CreatedAt",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CurrencyId",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_Gateway_GatewayReference",
                schema: "Commerce",
                table: "PaymentTransaction",
                columns: new[] { "Gateway", "GatewayReference" },
                filter: "[GatewayReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentMethodId",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentStatusId",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentStatusId_ExpiresAt",
                schema: "Commerce",
                table: "PaymentTransaction",
                columns: new[] { "PaymentStatusId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_TransactionNo",
                schema: "Commerce",
                table: "PaymentTransaction",
                column: "TransactionNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_UserId_CreatedAt",
                schema: "Commerce",
                table: "PaymentTransaction",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Code",
                schema: "Securities",
                table: "Permission",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_ScreenId",
                schema: "Securities",
                table: "Permission",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ReplacedByTokenId",
                schema: "Securities",
                table: "RefreshToken",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                schema: "Securities",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_ExpiresAt",
                schema: "Securities",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                schema: "Securities",
                table: "Role",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                schema: "Securities",
                table: "Role",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RoleScreen_RoleId_ScreenId",
                schema: "Securities",
                table: "RoleScreen",
                columns: new[] { "RoleId", "ScreenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleScreen_ScreenId",
                schema: "Securities",
                table: "RoleScreen",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_Screen_Code",
                schema: "Securities",
                table: "Screen",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Section_CourseId",
                schema: "Catalog",
                table: "Section",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Section_ParentSectionId_SortOrder",
                schema: "Catalog",
                table: "Section",
                columns: new[] { "ParentSectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Section_SubmoduleId_SortOrder",
                schema: "Catalog",
                table: "Section",
                columns: new[] { "SubmoduleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_DocumentTypeId",
                schema: "Catalog",
                table: "SectionDocument",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_MediaAssetId",
                schema: "Catalog",
                table: "SectionDocument",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_SectionId_SortOrder",
                schema: "Catalog",
                table: "SectionDocument",
                columns: new[] { "SectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_SectionVideoId",
                schema: "Catalog",
                table: "SectionDocument",
                column: "SectionVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_MediaAssetId",
                schema: "Catalog",
                table: "SectionVideo",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_SectionId_SortOrder",
                schema: "Catalog",
                table: "SectionVideo",
                columns: new[] { "SectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StreamingWebhookEvent_AssetId",
                schema: "Media",
                table: "StreamingWebhookEvent",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingWebhookEvent_EventId",
                schema: "Media",
                table: "StreamingWebhookEvent",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submodule_CourseModuleId_SortOrder",
                schema: "Catalog",
                table: "Submodule",
                columns: new[] { "CourseModuleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                schema: "Securities",
                table: "User",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_User_InstructorId",
                schema: "Securities",
                table: "User",
                column: "InstructorId",
                unique: true,
                filter: "[InstructorId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                schema: "Securities",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScreen_ScreenId",
                schema: "Securities",
                table: "UserScreen",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScreen_UserId_ScreenId",
                schema: "Securities",
                table: "UserScreen",
                columns: new[] { "UserId", "ScreenId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorySkill",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "CourseGrantPermission",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "CourseOutcome",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "CourseRequirement",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Enrollment",
                schema: "Commerce");

            migrationBuilder.DropTable(
                name: "InstructorExpertise",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "LessonProgress",
                schema: "Learning");

            migrationBuilder.DropTable(
                name: "LiveSession",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Material",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "RoleScreen",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "SectionDocument",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "StreamingWebhookEvent",
                schema: "Media");

            migrationBuilder.DropTable(
                name: "UserScreen",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "CourseGrant",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "EnrollmentStatus",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "PaymentTransaction",
                schema: "Commerce");

            migrationBuilder.DropTable(
                name: "MaterialType",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "DocumentType",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "SectionVideo",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Screen",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "Currency",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "PaymentMethod",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "PaymentStatus",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "MediaAsset",
                schema: "Media");

            migrationBuilder.DropTable(
                name: "Section",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "MediaAssetStatus",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "MediaKind",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "User",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "Submodule",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "Securities");

            migrationBuilder.DropTable(
                name: "CourseModule",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Course",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "CourseBadge",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "CourseFormat",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "CourseLevel",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "Instructor",
                schema: "Catalog");
        }
    }
}
