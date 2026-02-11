using System.ComponentModel.DataAnnotations;

namespace project_lifecycle.Models
{
    public class HumanResource
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string MiddleName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(20)]
        public string Contact { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? PositionId { get; set; }

        public Position? Position { get; set; }
    }
}
