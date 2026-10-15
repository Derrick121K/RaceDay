using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventType",
                columns: table => new
                {
                    EventTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventType", x => x.EventTypeID);
                    table.UniqueConstraint("UQ_EventType_TypeName", x => x.TypeName);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserID);
                    table.UniqueConstraint("UQ_User_Email", x => x.Email);
                    table.CheckConstraint("CK_User_Role", "[Role] IN ('ORGANISER', 'PARTICIPANT')");
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    EventDate = table.Column<DateTime>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Distance = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EventTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganiserID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.EventID);
                    table.CheckConstraint("CK_Event_Distance", "[Distance] > 0");
                    table.ForeignKey(
                        name: "FK_Event_EventType",
                        column: x => x.EventTypeID,
                        principalTable: "EventType",
                        principalColumn: "EventTypeID");
                    table.ForeignKey(
                        name: "FK_Event_Organiser",
                        column: x => x.OrganiserID,
                        principalTable: "User",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    MinimumAge = table.Column<int>(type: "int", nullable: true),
                    MaximumAge = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryID);
                    table.UniqueConstraint("UQ_Category_Event_Category", x => new { x.EventID, x.CategoryID });
                    table.UniqueConstraint("UQ_Category_Event_Name", x => new { x.EventID, x.CategoryName });
                    table.CheckConstraint("CK_Category_AgeRange", "[MinimumAge] IS NULL OR [MaximumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
                    table.CheckConstraint("CK_Category_MaximumAge", "[MaximumAge] IS NULL OR [MaximumAge] >= 0");
                    table.CheckConstraint("CK_Category_MinimumAge", "[MinimumAge] IS NULL OR [MinimumAge] >= 0");
                    table.ForeignKey(
                        name: "FK_Category_Event",
                        column: x => x.EventID,
                        principalTable: "Event",
                        principalColumn: "EventID");
                });

            migrationBuilder.CreateTable(
                name: "Enrolment",
                columns: table => new
                {
                    EnrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantID = table.Column<int>(type: "int", nullable: false),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolment", x => x.EnrolmentID);
                    table.UniqueConstraint("UQ_Enrolment_Participant_Event", x => new { x.ParticipantID, x.EventID });
                    table.CheckConstraint("CK_Enrolment_Status", "[Status] IN ('ACTIVE', 'CANCELLED', 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_Enrolment_Category",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_Enrolment_Event",
                        column: x => x.EventID,
                        principalTable: "Event",
                        principalColumn: "EventID");
                    table.ForeignKey(
                        name: "FK_Enrolment_Event_Category",
                        columns: x => new { x.EventID, x.CategoryID },
                        principalTable: "Category",
                        principalColumns: new[] { "EventID", "CategoryID" });
                    table.ForeignKey(
                        name: "FK_Enrolment_Participant",
                        column: x => x.ParticipantID,
                        principalTable: "User",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Result",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentID = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    FinishingPosition = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Result", x => x.ResultID);
                    table.UniqueConstraint("UQ_Result_Enrolment", x => x.EnrolmentID);
                    table.CheckConstraint("CK_Result_FinishingPosition", "[FinishingPosition] > 0");
                    table.ForeignKey(
                        name: "FK_Result_Enrolment",
                        column: x => x.EnrolmentID,
                        principalTable: "Enrolment",
                        principalColumn: "EnrolmentID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_CategoryID",
                table: "Enrolment",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_EventID_CategoryID",
                table: "Enrolment",
                columns: new[] { "EventID", "CategoryID" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventTypeID",
                table: "Event",
                column: "EventTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Event_OrganiserID",
                table: "Event",
                column: "OrganiserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Result");

            migrationBuilder.DropTable(
                name: "Enrolment");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "EventType");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
