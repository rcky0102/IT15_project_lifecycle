using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class Input_nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Employees_EmployeeId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_ProjectRoles_ProjectRoleId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Projects_ProjectId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMilestones_Milestones_MilestoneId",
                table: "ProjectMilestones");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMilestones_Projects_ProjectId",
                table: "ProjectMilestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectManagers_ProjectManagerId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskMembers_Members_MemberId",
                table: "TaskMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskMembers_ProjectTasks_ProjectTaskId",
                table: "TaskMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Input",
                table: "ProjectTasks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Employees_EmployeeId",
                table: "Members",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_ProjectRoles_ProjectRoleId",
                table: "Members",
                column: "ProjectRoleId",
                principalTable: "ProjectRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Projects_ProjectId",
                table: "Members",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMilestones_Milestones_MilestoneId",
                table: "ProjectMilestones",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMilestones_Projects_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "ProjectManagers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectManagers_ProjectManagerId",
                table: "ProjectTasks",
                column: "ProjectManagerId",
                principalTable: "ProjectManagers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId",
                table: "ProjectTasks",
                column: "ProjectMilestoneId",
                principalTable: "ProjectMilestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskMembers_Members_MemberId",
                table: "TaskMembers",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskMembers_ProjectTasks_ProjectTaskId",
                table: "TaskMembers",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Employees_EmployeeId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_ProjectRoles_ProjectRoleId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Projects_ProjectId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMilestones_Milestones_MilestoneId",
                table: "ProjectMilestones");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMilestones_Projects_ProjectId",
                table: "ProjectMilestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectManagers_ProjectManagerId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskMembers_Members_MemberId",
                table: "TaskMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskMembers_ProjectTasks_ProjectTaskId",
                table: "TaskMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Input",
                table: "ProjectTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Employees_EmployeeId",
                table: "Members",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_ProjectRoles_ProjectRoleId",
                table: "Members",
                column: "ProjectRoleId",
                principalTable: "ProjectRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Projects_ProjectId",
                table: "Members",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMilestones_Milestones_MilestoneId",
                table: "ProjectMilestones",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMilestones_Projects_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "ProjectManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectManagers_ProjectManagerId",
                table: "ProjectTasks",
                column: "ProjectManagerId",
                principalTable: "ProjectManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId",
                table: "ProjectTasks",
                column: "ProjectMilestoneId",
                principalTable: "ProjectMilestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskMembers_Members_MemberId",
                table: "TaskMembers",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskMembers_ProjectTasks_ProjectTaskId",
                table: "TaskMembers",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
