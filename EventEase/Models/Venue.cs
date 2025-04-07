using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    [Table("Venue")]
    public class Venue
    {
        [Key]
        public int VenueId { get; set; }

        [Required]
        [StringLength(100)]
        public string VenueName { get; set; }

        [Required]
        [StringLength(255)]
        public string Location { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Url]
        public string ImageUrl { get; set; }
    }
}