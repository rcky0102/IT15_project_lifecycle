using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using project_lifecycle.Data;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260216135000_AddDepartmentHeadAndNoteToProjectProposal")]
    public partial class AddDepartmentHeadAndNoteToProjectProposal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentHeadId",
                table: "ProjectProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ProjectProposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProposals_DepartmentHeadId",
                table: "ProjectProposals",
                column: "DepartmentHeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectProposals_DepartmentHeads_DepartmentHeadId",
                table: "ProjectProposals",
                column: "DepartmentHeadId",
                principalTable: "DepartmentHeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectProposals_DepartmentHeads_DepartmentHeadId",
                table: "ProjectProposals");

            migrationBuilder.DropIndex(
                name: "IX_ProjectProposals_DepartmentHeadId",
                table: "ProjectProposals");

            migrationBuilder.DropColumn(
                name: "DepartmentHeadId",
                table: "ProjectProposals");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ProjectProposals");
        }
    }
}
