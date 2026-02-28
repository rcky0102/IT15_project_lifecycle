using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class DocumentVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public Document? Document { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = string.Empty;

        /// <summary>The employee who saved this version.</summary>
        [Required]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
