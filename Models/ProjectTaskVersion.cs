using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProjectTaskVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectTaskId { get; set; }
        [ForeignKey("ProjectTaskId")]
        public ProjectTask? ProjectTask { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Input { get; set; }

        [Required]
        public int TaskMemberId { get; set; }
        [ForeignKey("TaskMemberId")]
        public TaskMember? TaskMember { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
