using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class RulesApplied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "HumanResources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Executives",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HumanResources_DepartmentId",
                table: "HumanResources",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Executives_DepartmentId",
                table: "Executives",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Executives_Departments_DepartmentId",
                table: "Executives",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HumanResources_Departments_DepartmentId",
                table: "HumanResources",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Executives_Departments_DepartmentId",
                table: "Executives");

            migrationBuilder.DropForeignKey(
                name: "FK_HumanResources_Departments_DepartmentId",
                table: "HumanResources");

            migrationBuilder.DropIndex(
                name: "IX_HumanResources_DepartmentId",
                table: "HumanResources");

            migrationBuilder.DropIndex(
                name: "IX_Executives_DepartmentId",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Executives");
        }
    }
}
