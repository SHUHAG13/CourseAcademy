using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonProgress",
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
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_SectionVideo_SectionVideoId",
                        column: x => x.SectionVideoId,
                        principalTable: "SectionVideo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_Section_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Section",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonProgress_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_CourseId",
                table: "LessonProgress",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_SectionId",
                table: "LessonProgress",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_SectionVideoId",
                table: "LessonProgress",
                column: "SectionVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_CourseId",
                table: "LessonProgress",
                columns: new[] { "UserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_SectionId_SectionVideoId",
                table: "LessonProgress",
                columns: new[] { "UserId", "SectionId", "SectionVideoId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonProgress");
        }
    }
}
