using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class DocumentCollaborator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public Document? Document { get; set; }

        /// <summary>The employee who has been invited to collaborate.</summary>
        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        /// <summary>Role of the collaborator: Editor or Viewer.</summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Editor";

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
