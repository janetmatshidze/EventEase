using EventEase.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventEase.Models.ViewModels
{
    public class EventViewModel
    {
        public List<Event> UpcomingEvents { get; set; }
        public List<Event> PastEvents { get; set; }

        public EventViewModel(List<Event> events)
        {
            var currentDate = DateTime.Today;

            UpcomingEvents = events
                .Where(e => e.EventDate >= currentDate)
                .OrderBy(e => e.EventDate)
                .ToList();

            PastEvents = events
                .Where(e => e.EventDate < currentDate)
                .OrderByDescending(e => e.EventDate)
                .ToList();
        }
    }
}