using System.ComponentModel.DataAnnotations;

namespace project_lifecycle.Models
{
    public class Executive
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(20)]
        public string Contact { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? PositionId { get; set; }
        public Position? Position { get; set; }

        [StringLength(200)]
        public string? AddressLine { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Barangay { get; set; }

        [StringLength(500)]
        public string? ProfileImage { get; set; }
    }
}
