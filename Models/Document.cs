using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The employee who created (owns) this document.</summary>
        [Required]
        public int OwnerEmployeeId { get; set; }
        [ForeignKey("OwnerEmployeeId")]
        public Employee? OwnerEmployee { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Rich-text HTML content produced by CKEditor.</summary>
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = string.Empty;

        /// <summary>Indicates whether the document has been archived.</summary>
        public bool IsArchived { get; set; } = false;

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? LastModified { get; set; }
    }
}
