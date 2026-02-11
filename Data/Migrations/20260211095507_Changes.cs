using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "ProjectManagers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "HumanResources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Executives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "DepartmentHeads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManagers_PositionId",
                table: "ProjectManagers",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanResources_PositionId",
                table: "HumanResources",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Executives_PositionId",
                table: "Executives",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentHeads_PositionId",
                table: "DepartmentHeads",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentHeads_Positions_PositionId",
                table: "DepartmentHeads",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Executives_Positions_PositionId",
                table: "Executives",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanResources_Positions_PositionId",
                table: "HumanResources",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectManagers_Positions_PositionId",
                table: "ProjectManagers",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentHeads_Positions_PositionId",
                table: "DepartmentHeads");

            migrationBuilder.DropForeignKey(
                name: "FK_Executives_Positions_PositionId",
                table: "Executives");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanResources_Positions_PositionId",
                table: "HumanResources");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectManagers_Positions_PositionId",
                table: "ProjectManagers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectManagers_PositionId",
                table: "ProjectManagers");

            migrationBuilder.DropIndex(
                name: "IX_HumanResources_PositionId",
                table: "HumanResources");

            migrationBuilder.DropIndex(
                name: "IX_Executives_PositionId",
                table: "Executives");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentHeads_PositionId",
                table: "DepartmentHeads");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "DepartmentHeads");
        }
    }
}
