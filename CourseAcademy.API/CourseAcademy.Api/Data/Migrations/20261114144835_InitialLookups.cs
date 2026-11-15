using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
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
                name: "MaterialType",
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
                name: "PaymentMethod",
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

            migrationBuilder.InsertData(
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
                table: "CourseBadge",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Bestseller", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bestseller", 1, null },
                    { 2, "New", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New", 2, null },
                    { 3, "Popular", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Popular", 3, null }
                });

            migrationBuilder.InsertData(
                table: "CourseFormat",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Pre-recorded", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pre-recorded", 1, null },
                    { 2, "Live", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Live", 2, null },
                    { 3, "Hybrid", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hybrid", 3, null }
                });

            migrationBuilder.InsertData(
                table: "CourseLevel",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Beginner", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Beginner", 1, null },
                    { 2, "Intermediate", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Intermediate", 2, null },
                    { 3, "Advanced", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced", 3, null }
                });

            migrationBuilder.InsertData(
                table: "Currency",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[] { 1, "BDT", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bangladeshi Taka", 1, null });

            migrationBuilder.InsertData(
                table: "DocumentType",
                columns: new[] { "Id", "Code", "CreatedAt", "FileExtension", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "PDF", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".pdf", "PDF", 1, null },
                    { 2, "DOCX", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".docx", "Word document", 2, null },
                    { 3, "PPTX", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), ".pptx", "PowerPoint slides", 3, null }
                });

            migrationBuilder.InsertData(
                table: "EnrollmentStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "active", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active", 1, null },
                    { 2, "revoked", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Revoked", 2, null }
                });

            migrationBuilder.InsertData(
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

            migrationBuilder.CreateIndex(
                name: "IX_Category_Code",
                table: "Category",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBadge_Code",
                table: "CourseBadge",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFormat_Code",
                table: "CourseFormat",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseLevel_Code",
                table: "CourseLevel",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_Code",
                table: "Currency",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_Code",
                table: "DocumentType",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatus_Code",
                table: "EnrollmentStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialType_Code",
                table: "MaterialType",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Code",
                table: "PaymentMethod",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatus_Code",
                table: "PaymentStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "CourseBadge");

            migrationBuilder.DropTable(
                name: "CourseFormat");

            migrationBuilder.DropTable(
                name: "CourseLevel");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "DocumentType");

            migrationBuilder.DropTable(
                name: "EnrollmentStatus");

            migrationBuilder.DropTable(
                name: "MaterialType");

            migrationBuilder.DropTable(
                name: "PaymentMethod");

            migrationBuilder.DropTable(
                name: "PaymentStatus");
        }
    }
}
