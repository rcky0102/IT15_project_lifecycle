using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using project_lifecycle.Models;

namespace project_lifecycle.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User Role")]
        public string Role { get; set; } = string.Empty;

        // Employee specific fields
        [Required]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Position")]
        public int? PositionId { get; set; }

        [Display(Name = "Date Hired")]
        public DateTime DateHired { get; set; } = DateTime.Today;

        [Display(Name = "Contact")]
        public string? Contact { get; set; }

        // Lists for dropdowns
        public List<Department> Departments { get; set; } = new();
        public List<Position> Positions { get; set; } = new();
    }
}
