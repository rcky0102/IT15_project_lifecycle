using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace project_lifecycle.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionTitle { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public string? AddressLine { get; set; }
        public string? Region { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; }
        public string? ProfileImage { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? DateHired { get; set; }
        public DateTime? CreatedDate { get; set; }

        // For editing
        public IFormFile? ProfileImageFile { get; set; }

        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";
    }
}
