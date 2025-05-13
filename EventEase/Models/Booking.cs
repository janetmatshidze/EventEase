using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    [Table("Booking")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Please select an event")]
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Please select a venue")]
        public int? VenueId { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Booking date is required")]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // Removed Required attribute since these are populated from Event and Venue
        [StringLength(100)]
        public string EventName { get; set; }

        [StringLength(100)]
        public string VenueName { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [ForeignKey("VenueId")]
        public virtual Venue? Venue { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}