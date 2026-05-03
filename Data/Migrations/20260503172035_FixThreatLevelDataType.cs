using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixThreatLevelDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing string values to integer threat levels
            migrationBuilder.Sql(@"
                UPDATE SecurityLogs 
                SET ThreatLevel = CASE 
                    WHEN ThreatLevel = 'Information' THEN 1
                    WHEN ThreatLevel = 'Warning' THEN 2
                    WHEN ThreatLevel = 'Error' THEN 3
                    WHEN ThreatLevel = 'Fatal' THEN 4
                    ELSE 1
                END
                WHERE ThreatLevel IN ('Information', 'Warning', 'Error', 'Fatal')
            ");

            // Alter column to int type
            migrationBuilder.AlterColumn<int>(
                name: "ThreatLevel",
                table: "SecurityLogs",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes - convert back to nvarchar
            migrationBuilder.AlterColumn<string>(
                name: "ThreatLevel",
                table: "SecurityLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Information");

            // Convert integer values back to strings
            migrationBuilder.Sql(@"
                UPDATE SecurityLogs 
                SET ThreatLevel = CASE 
                    WHEN ThreatLevel = 1 THEN 'Information'
                    WHEN ThreatLevel = 2 THEN 'Warning'
                    WHEN ThreatLevel = 3 THEN 'Error'
                    WHEN ThreatLevel = 4 THEN 'Fatal'
                    ELSE 'Information'
                END
                WHERE ThreatLevel IN (1, 2, 3, 4)
            ");
        }
    }
}
