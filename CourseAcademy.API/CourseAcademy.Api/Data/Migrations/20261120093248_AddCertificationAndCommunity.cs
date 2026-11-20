using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationAndCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Certification");

            migrationBuilder.EnsureSchema(
                name: "Community");

            migrationBuilder.CreateTable(
                name: "CertificationMode",
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
                    table.PrimaryKey("PK_CertificationMode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamType",
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
                    table.PrimaryKey("PK_ExamType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Post",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Post_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Post_User_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmissionStatus",
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
                    table.PrimaryKey("PK_SubmissionStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Certificate",
                schema: "Certification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "char(14)", unicode: false, fixedLength: true, maxLength: 14, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CourseTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CertificationModeId = table.Column<int>(type: "int", nullable: false),
                    ExamTypeId = table.Column<int>(type: "int", nullable: true),
                    ScorePercent = table.Column<int>(type: "int", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificate", x => x.Id);
                    table.CheckConstraint("CK_Certificate_ScorePercent", "[ScorePercent] IS NULL OR [ScorePercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Certificate_CertificationMode_CertificationModeId",
                        column: x => x.CertificationModeId,
                        principalSchema: "Lookups",
                        principalTable: "CertificationMode",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Certificate_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Certificate_ExamType_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalSchema: "Lookups",
                        principalTable: "ExamType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Certificate_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CertificationRule",
                schema: "Certification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CertificationModeId = table.Column<int>(type: "int", nullable: false),
                    MinProgress = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    ExamTypeId = table.Column<int>(type: "int", nullable: false),
                    ExamTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExamInstructions = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: true),
                    MaxScore = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    PassingScore = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationRule", x => x.Id);
                    table.CheckConstraint("CK_CertificationRule_MaxAttempts", "[MaxAttempts] >= 0");
                    table.CheckConstraint("CK_CertificationRule_MinProgress", "[MinProgress] BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_CertificationRule_Scores", "[MaxScore] >= 1 AND [PassingScore] BETWEEN 1 AND [MaxScore]");
                    table.ForeignKey(
                        name: "FK_CertificationRule_CertificationMode_CertificationModeId",
                        column: x => x.CertificationModeId,
                        principalSchema: "Lookups",
                        principalTable: "CertificationMode",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CertificationRule_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CertificationRule_ExamType_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalSchema: "Lookups",
                        principalTable: "ExamType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comment_Comment_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalSchema: "Community",
                        principalTable: "Comment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comment_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comment_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "Community",
                        principalTable: "Post",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comment_User_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamSubmission",
                schema: "Certification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    ExamTypeId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    SubmissionStatusId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubmission", x => x.Id);
                    table.CheckConstraint("CK_ExamSubmission_Score", "[Score] IS NULL OR [Score] >= 0");
                    table.ForeignKey(
                        name: "FK_ExamSubmission_Course_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Catalog",
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamSubmission_ExamType_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalSchema: "Lookups",
                        principalTable: "ExamType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamSubmission_SubmissionStatus_SubmissionStatusId",
                        column: x => x.SubmissionStatusId,
                        principalSchema: "Lookups",
                        principalTable: "SubmissionStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamSubmission_User_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamSubmission_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "Securities",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "CertificationMode",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "completion", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Course completion", 1, null },
                    { 2, "exam", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Central exam", 2, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "ExamType",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "project", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Project", 1, null },
                    { 2, "presentation", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Presentation", 2, null }
                });

            migrationBuilder.InsertData(
                schema: "Lookups",
                table: "SubmissionStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "submitted", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Waiting for review", 1, null },
                    { 2, "passed", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Passed", 2, null },
                    { 3, "failed", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Not passed", 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_CertificationModeId",
                schema: "Certification",
                table: "Certificate",
                column: "CertificationModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_Code",
                schema: "Certification",
                table: "Certificate",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_CourseId",
                schema: "Certification",
                table: "Certificate",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_ExamTypeId",
                schema: "Certification",
                table: "Certificate",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificate_UserId_CourseId",
                schema: "Certification",
                table: "Certificate",
                columns: new[] { "UserId", "CourseId" },
                unique: true,
                filter: "[IsRevoked] = 0 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationMode_Code",
                schema: "Lookups",
                table: "CertificationMode",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRule_CertificationModeId",
                schema: "Certification",
                table: "CertificationRule",
                column: "CertificationModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRule_CourseId",
                schema: "Certification",
                table: "CertificationRule",
                column: "CourseId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRule_ExamTypeId",
                schema: "Certification",
                table: "CertificationRule",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_AuthorUserId",
                schema: "Community",
                table: "Comment",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_CourseId_CreatedAt",
                schema: "Community",
                table: "Comment",
                columns: new[] { "CourseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ParentCommentId",
                schema: "Community",
                table: "Comment",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostId_CreatedAt",
                schema: "Community",
                table: "Comment",
                columns: new[] { "PostId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmission_CourseId_SubmissionStatusId_SubmittedAt",
                schema: "Certification",
                table: "ExamSubmission",
                columns: new[] { "CourseId", "SubmissionStatusId", "SubmittedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmission_ExamTypeId",
                schema: "Certification",
                table: "ExamSubmission",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmission_ReviewedByUserId",
                schema: "Certification",
                table: "ExamSubmission",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmission_SubmissionStatusId",
                schema: "Certification",
                table: "ExamSubmission",
                column: "SubmissionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmission_UserId_CourseId",
                schema: "Certification",
                table: "ExamSubmission",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamType_Code",
                schema: "Lookups",
                table: "ExamType",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Post_AuthorUserId",
                schema: "Community",
                table: "Post",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_CourseId_IsPinned_CreatedAt",
                schema: "Community",
                table: "Post",
                columns: new[] { "CourseId", "IsPinned", "CreatedAt" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStatus_Code",
                schema: "Lookups",
                table: "SubmissionStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificate",
                schema: "Certification");

            migrationBuilder.DropTable(
                name: "CertificationRule",
                schema: "Certification");

            migrationBuilder.DropTable(
                name: "Comment",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "ExamSubmission",
                schema: "Certification");

            migrationBuilder.DropTable(
                name: "CertificationMode",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "Post",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "ExamType",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "SubmissionStatus",
                schema: "Lookups");
        }
    }
}
