using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class TaskNoteVersion
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
        public string? Note { get; set; }

        [Required]
        public int ProjectManagerId { get; set; }
        [ForeignKey("ProjectManagerId")]
        public ProjectManager? ProjectManager { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
