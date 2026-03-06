using System.ComponentModel.DataAnnotations;

namespace project_lifecycle.Models
{
    public class Position
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public bool IsArchived { get; set; } = false;
    }
}
