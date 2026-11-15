using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseAcademy.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Commerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
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
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_PaymentMethod_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethod",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_PaymentStatus_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalTable: "PaymentStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollment",
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
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_EnrollmentStatus_EnrollmentStatusId",
                        column: x => x.EnrollmentStatusId,
                        principalTable: "EnrollmentStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_PaymentTransaction_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_CourseId_EnrollmentStatusId",
                table: "Enrollment",
                columns: new[] { "CourseId", "EnrollmentStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_EnrollmentStatusId",
                table: "Enrollment",
                column: "EnrollmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_PaymentTransactionId",
                table: "Enrollment",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_UserId_CourseId",
                table: "Enrollment",
                columns: new[] { "UserId", "CourseId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CourseId",
                table: "PaymentTransaction",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CreatedAt",
                table: "PaymentTransaction",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CurrencyId",
                table: "PaymentTransaction",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_Gateway_GatewayReference",
                table: "PaymentTransaction",
                columns: new[] { "Gateway", "GatewayReference" },
                filter: "[GatewayReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentMethodId",
                table: "PaymentTransaction",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentStatusId",
                table: "PaymentTransaction",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentStatusId_ExpiresAt",
                table: "PaymentTransaction",
                columns: new[] { "PaymentStatusId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_TransactionNo",
                table: "PaymentTransaction",
                column: "TransactionNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_UserId_CreatedAt",
                table: "PaymentTransaction",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollment");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");
        }
    }
}
