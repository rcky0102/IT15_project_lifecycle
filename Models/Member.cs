using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public int ProjectRoleId { get; set; }
        [ForeignKey("ProjectRoleId")]
        public ProjectRole? ProjectRole { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
    }
}
