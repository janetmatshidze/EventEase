using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    [Table("Event")]
    public class Event
    {
        [Key]
        public int EventId { get; set; }

       
        [StringLength(100)]
        public string EventName { get; set; }

      
        public DateTime EventDate { get; set; }

        public string Description { get; set; }

        // Store the image URL in the database
        [Display(Name = "Event Image")]
        public string ImageFile { get; set; }

        // This property is not mapped to the database
        [NotMapped]
        [Display(Name = "Upload Image")]
        public IFormFile ImageUpload { get; set; }

    }
}