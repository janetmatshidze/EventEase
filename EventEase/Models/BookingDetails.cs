using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class BookingDetails
    {
        [Key]
        public int BookingId { get; set; }
        public int? EventId { get; set; }
        public string EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public string EventDescription { get; set; }
        //public string EventImageFile { get; set; }
        public int? VenueId { get; set; }
        public string VenueName { get; set; }
        public string VenueLocation { get; set; }
        public int VenueCapacity { get; set; }
        //public string VenueImageFile { get; set; }
        public int? UserId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BookingDate { get; set; }
    }
}