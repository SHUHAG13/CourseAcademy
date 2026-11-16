using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Media : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SectionVideo_AssetId",
                table: "SectionVideo");

            migrationBuilder.DropIndex(
                name: "IX_SectionDocument_AssetId",
                table: "SectionDocument");

            migrationBuilder.DropIndex(
                name: "IX_Material_AssetId",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "SectionVideo");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "SectionDocument");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Material");

            migrationBuilder.AddColumn<int>(
                name: "MediaAssetId",
                table: "SectionVideo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaAssetId",
                table: "SectionDocument",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaAssetId",
                table: "Material",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaAssetStatus",
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
                name: "StreamingWebhookEvent",
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
                name: "MediaAsset",
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
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_MediaAssetStatus_MediaAssetStatusId",
                        column: x => x.MediaAssetStatusId,
                        principalTable: "MediaAssetStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_MediaKind_MediaKindId",
                        column: x => x.MediaKindId,
                        principalTable: "MediaKind",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaAsset_User_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
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
                table: "MediaKind",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "video", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Video", 1, null },
                    { 2, "document", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Document", 2, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_MediaAssetId",
                table: "SectionVideo",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_MediaAssetId",
                table: "SectionDocument",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Material_MediaAssetId",
                table: "Material",
                column: "MediaAssetId",
                filter: "[MediaAssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_AssetId",
                table: "MediaAsset",
                column: "AssetId",
                unique: true,
                filter: "[AssetId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_CourseId",
                table: "MediaAsset",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_MediaAssetStatusId_DeleteRequestedAt",
                table: "MediaAsset",
                columns: new[] { "MediaAssetStatusId", "DeleteRequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_MediaKindId",
                table: "MediaAsset",
                column: "MediaKindId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_UploadedByUserId",
                table: "MediaAsset",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetStatus_Code",
                table: "MediaAssetStatus",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MediaKind_Code",
                table: "MediaKind",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingWebhookEvent_AssetId",
                table: "StreamingWebhookEvent",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingWebhookEvent_EventId",
                table: "StreamingWebhookEvent",
                column: "EventId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Material_MediaAsset_MediaAssetId",
                table: "Material",
                column: "MediaAssetId",
                principalTable: "MediaAsset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionDocument_MediaAsset_MediaAssetId",
                table: "SectionDocument",
                column: "MediaAssetId",
                principalTable: "MediaAsset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionVideo_MediaAsset_MediaAssetId",
                table: "SectionVideo",
                column: "MediaAssetId",
                principalTable: "MediaAsset",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Material_MediaAsset_MediaAssetId",
                table: "Material");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionDocument_MediaAsset_MediaAssetId",
                table: "SectionDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionVideo_MediaAsset_MediaAssetId",
                table: "SectionVideo");

            migrationBuilder.DropTable(
                name: "MediaAsset");

            migrationBuilder.DropTable(
                name: "StreamingWebhookEvent");

            migrationBuilder.DropTable(
                name: "MediaAssetStatus");

            migrationBuilder.DropTable(
                name: "MediaKind");

            migrationBuilder.DropIndex(
                name: "IX_SectionVideo_MediaAssetId",
                table: "SectionVideo");

            migrationBuilder.DropIndex(
                name: "IX_SectionDocument_MediaAssetId",
                table: "SectionDocument");

            migrationBuilder.DropIndex(
                name: "IX_Material_MediaAssetId",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "MediaAssetId",
                table: "SectionVideo");

            migrationBuilder.DropColumn(
                name: "MediaAssetId",
                table: "SectionDocument");

            migrationBuilder.DropColumn(
                name: "MediaAssetId",
                table: "Material");

            migrationBuilder.AddColumn<string>(
                name: "AssetId",
                table: "SectionVideo",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetId",
                table: "SectionDocument",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetId",
                table: "Material",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionVideo_AssetId",
                table: "SectionVideo",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SectionDocument_AssetId",
                table: "SectionDocument",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Material_AssetId",
                table: "Material",
                column: "AssetId",
                filter: "[AssetId] IS NOT NULL");
        }
    }
}
