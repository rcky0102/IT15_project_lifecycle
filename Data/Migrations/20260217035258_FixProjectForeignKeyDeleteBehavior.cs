using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixProjectForeignKeyDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectProposals_ProjectProposalId",
                table: "Projects");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectProposals_ProjectProposalId",
                table: "Projects",
                column: "ProjectProposalId",
                principalTable: "ProjectProposals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectProposals_ProjectProposalId",
                table: "Projects");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectProposals_ProjectProposalId",
                table: "Projects",
                column: "ProjectProposalId",
                principalTable: "ProjectProposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
