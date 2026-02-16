using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProjectProposal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Input { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Pending|Rejected|Approved|Requires Revision)$", ErrorMessage = "Status must be one of: Pending, Rejected, Approved, Requires Revision.")]
        public string Status { get; set; } = "Pending";

        // Optional: reference to the department head who reviewed or is assigned to this proposal
        public int? DepartmentHeadId { get; set; }
        [ForeignKey("DepartmentHeadId")]
        public DepartmentHead? DepartmentHead { get; set; }

        // Optional internal note about the proposal or review comments
        [Column(TypeName = "nvarchar(max)")]
        public string? Note { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
