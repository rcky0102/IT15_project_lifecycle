using Microsoft.AspNetCore.Identity;
using project_lifecycle.Models;

namespace project_lifecycle.ViewModels
{
    public class UserListViewModel
    {
        public List<UserDetailsViewModel> Users { get; set; } = new();
        public CreateUserViewModel CreateUserViewModel { get; set; } = new();
    }

    public class UserDetailsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? EmployeeNumber { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? DepartmentName { get; set; }
        public string? Contact { get; set; }
        public string? AddressLine { get; set; }
        public string? Region { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; }
        public string? PositionName { get; set; }
        public DateTime? DateHired { get; set; }
        public string? ProfileImage { get; set; }
    }
}
