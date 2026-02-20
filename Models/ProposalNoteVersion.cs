using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_lifecycle.Models
{
    public class ProposalNoteVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectProposalId { get; set; }
        [ForeignKey("ProjectProposalId")]
        public ProjectProposal? ProjectProposal { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Note { get; set; }

        public int? DepartmentHeadId { get; set; }
        [ForeignKey("DepartmentHeadId")]
        public DepartmentHead? DepartmentHead { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
