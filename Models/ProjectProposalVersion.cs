using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProjectProposalVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectProposalId { get; set; }
        [ForeignKey("ProjectProposalId")]
        public ProjectProposal? ProjectProposal { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Input { get; set; } = string.Empty;

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
