using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSecurityLogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename existing columns to match our model
            migrationBuilder.RenameColumn(
                name: "Level",
                table: "SecurityLogs",
                newName: "ThreatLevel");

            migrationBuilder.RenameColumn(
                name: "Properties",
                table: "SecurityLogs",
                newName: "EventProperties");

            // Add missing columns
            migrationBuilder.AddColumn<bool>(
                name: "AccountLockedOut",
                table: "SecurityLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountLockoutTime",
                table: "SecurityLogs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            migrationBuilder.DropColumn(
                name: "AccountLockedOut",
                table: "SecurityLogs");

            migrationBuilder.DropColumn(
                name: "AccountLockoutTime",
                table: "SecurityLogs");

            migrationBuilder.RenameColumn(
                name: "ThreatLevel",
                table: "SecurityLogs",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "EventProperties",
                table: "SecurityLogs",
                newName: "Properties");
        }
    }
}
