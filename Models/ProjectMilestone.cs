using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProjectMilestone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required]
        public int MilestoneId { get; set; }
        [ForeignKey("MilestoneId")]
        public Milestone? Milestone { get; set; }

        [Required]
        public int SequenceOrder { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Unfinished|Finished)$", ErrorMessage = "Status must be Unfinished or Finished.")]
        public string Status { get; set; } = "Unfinished";

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
