using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProjectTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectMilestoneId { get; set; }
        [ForeignKey("ProjectMilestoneId")]
        public ProjectMilestone? ProjectMilestone { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Input { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Instructions { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Pending|Checked|Require Revision)$", ErrorMessage = "Status must be Pending, Checked or Require Revision.")]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }
        [ForeignKey("ProjectManagerId")]
        public ProjectManager? ProjectManager { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // When task was completed (set when Status transitions to "Checked")
        public DateTime? CompletedAt { get; set; }
    }
}
