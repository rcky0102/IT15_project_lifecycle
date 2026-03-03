using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectProposalId { get; set; }
        [ForeignKey("ProjectProposalId")]
        public ProjectProposal? ProjectProposal { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }
        [ForeignKey("ProjectManagerId")]
        public ProjectManager? ProjectManager { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Project status: Unfinished or Finished
        [Required]
        [StringLength(20)]
        [RegularExpression("^(Unfinished|Finished)$", ErrorMessage = "Status must be Unfinished or Finished.")]
        public string Status { get; set; } = "Unfinished";

        // Indicates whether the project has been archived and should be hidden from active lists
        public bool IsArchived { get; set; } = false;
    }
}
