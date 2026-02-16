using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class Changes_to_the_ProjectProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ProjectProposals");

            migrationBuilder.DropColumn(
                name: "FileAttachment",
                table: "ProjectProposals");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "ProjectProposals");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ProjectProposals",
                newName: "Input");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Input",
                table: "ProjectProposals",
                newName: "Description");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ProjectProposals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileAttachment",
                table: "ProjectProposals",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "ProjectProposals",
                type: "datetime2",
                nullable: true);
        }
    }
}
