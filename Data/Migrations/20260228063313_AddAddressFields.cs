using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project_lifecycle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "ProjectManagers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "ProjectManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ProjectManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "ProjectManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "ProjectManagers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "HumanResources",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "HumanResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "HumanResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "HumanResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "HumanResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Executives",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "Executives",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Executives",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Executives",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Executives",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "DepartmentHeads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "DepartmentHeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "DepartmentHeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "DepartmentHeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "DepartmentHeads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "ProjectManagers");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "City",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "HumanResources");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Executives");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "DepartmentHeads");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "DepartmentHeads");

            migrationBuilder.DropColumn(
                name: "City",
                table: "DepartmentHeads");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "DepartmentHeads");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "DepartmentHeads");
        }
    }
}
