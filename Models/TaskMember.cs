using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class TaskMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectTaskId { get; set; }
        [ForeignKey("ProjectTaskId")]
        public ProjectTask? ProjectTask { get; set; }

        [Required]
        public int MemberId { get; set; }
        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public bool IsArchived { get; set; } = false;

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
