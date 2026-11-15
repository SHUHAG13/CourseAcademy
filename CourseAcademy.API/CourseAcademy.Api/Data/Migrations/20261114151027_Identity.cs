using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
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
                name: "User",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
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
                        principalTable: "Screen",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleScreen",
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
                        principalTable: "Role",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoleScreen_Screen_ScreenId",
                        column: x => x.ScreenId,
                        principalTable: "Screen",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseGrant",
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
                        name: "FK_CourseGrant_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
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
                        principalTable: "RefreshToken",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserScreen",
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
                        principalTable: "Screen",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserScreen_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseGrantPermission",
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
                        principalTable: "CourseGrant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseGrantPermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsSystem", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "admin", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full access to every part of the academy.", true, "Admin", null },
                    { 2, "instructor", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Opens the admin panel for the courses they are granted.", true, "Instructor", null },
                    { 3, "user", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Learns in the courses they are enrolled in.", true, "Student", null }
                });

            migrationBuilder.InsertData(
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
                name: "IX_CourseGrant_CourseId",
                table: "CourseGrant",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrant_UserId_CourseId",
                table: "CourseGrant",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrantPermission_CourseGrantId_PermissionId",
                table: "CourseGrantPermission",
                columns: new[] { "CourseGrantId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrantPermission_PermissionId",
                table: "CourseGrantPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Code",
                table: "Permission",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_ScreenId",
                table: "Permission",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ReplacedByTokenId",
                table: "RefreshToken",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_ExpiresAt",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                table: "Role",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                table: "Role",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RoleScreen_RoleId_ScreenId",
                table: "RoleScreen",
                columns: new[] { "RoleId", "ScreenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleScreen_ScreenId",
                table: "RoleScreen",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_Screen_Code",
                table: "Screen",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScreen_ScreenId",
                table: "UserScreen",
                column: "ScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScreen_UserId_ScreenId",
                table: "UserScreen",
                columns: new[] { "UserId", "ScreenId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseGrantPermission");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "RoleScreen");

            migrationBuilder.DropTable(
                name: "UserScreen");

            migrationBuilder.DropTable(
                name: "CourseGrant");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Screen");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
