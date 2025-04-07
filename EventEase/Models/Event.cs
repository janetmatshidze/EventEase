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

        [Url]
        public string ImageUrl { get; set; }

    }
}