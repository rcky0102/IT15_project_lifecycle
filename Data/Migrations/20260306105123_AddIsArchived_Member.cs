using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchived_Member : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Members");
        }
    }
}
