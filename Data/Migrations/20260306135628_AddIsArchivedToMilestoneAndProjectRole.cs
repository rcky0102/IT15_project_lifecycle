using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToMilestoneAndProjectRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Milestones]') AND name = 'IsArchived')
    BEGIN
        ALTER TABLE [Milestones] ADD [IsArchived] bit NOT NULL DEFAULT 0;
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRoles]') AND name = 'IsArchived')
    BEGIN
        ALTER TABLE [ProjectRoles] ADD [IsArchived] bit NOT NULL DEFAULT 0;
    END
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql(@"
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Milestones]') AND name = 'IsArchived')
    BEGIN
        ALTER TABLE [Milestones] DROP COLUMN [IsArchived];
    END
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRoles]') AND name = 'IsArchived')
    BEGIN
        ALTER TABLE [ProjectRoles] DROP COLUMN [IsArchived];
    END
    ");
        }
    }
}
