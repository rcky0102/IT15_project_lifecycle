using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProposalNoteVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProposalNoteVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectProposalId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentHeadId = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalNoteVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalNoteVersions_DepartmentHeads_DepartmentHeadId",
                        column: x => x.DepartmentHeadId,
                        principalTable: "DepartmentHeads",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProposalNoteVersions_ProjectProposals_ProjectProposalId",
                        column: x => x.ProjectProposalId,
                        principalTable: "ProjectProposals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalNoteVersions_DepartmentHeadId",
                table: "ProposalNoteVersions",
                column: "DepartmentHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalNoteVersions_ProjectProposalId",
                table: "ProposalNoteVersions",
                column: "ProjectProposalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposalNoteVersions");
        }
    }
}
