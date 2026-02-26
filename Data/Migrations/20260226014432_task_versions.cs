using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class task_versions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTaskVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTaskId = table.Column<int>(type: "int", nullable: false),
                    Input = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskMemberId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTaskVersions_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTaskVersions_TaskMembers_TaskMemberId",
                        column: x => x.TaskMemberId,
                        principalTable: "TaskMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskNoteVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTaskId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectManagerId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNoteVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskNoteVersions_ProjectManagers_ProjectManagerId",
                        column: x => x.ProjectManagerId,
                        principalTable: "ProjectManagers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskNoteVersions_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskVersions_ProjectTaskId",
                table: "ProjectTaskVersions",
                column: "ProjectTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskVersions_TaskMemberId",
                table: "ProjectTaskVersions",
                column: "TaskMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNoteVersions_ProjectManagerId",
                table: "TaskNoteVersions",
                column: "ProjectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNoteVersions_ProjectTaskId",
                table: "TaskNoteVersions",
                column: "ProjectTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTaskVersions");

            migrationBuilder.DropTable(
                name: "TaskNoteVersions");
        }
    }
}
