using EVA.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Models
{
    public class EventViewModel
    {
      
        public int EventId { get; set; }
        public string Title { get; set; }
        public DateTime EventStartDateTime { get; set; }
        public DateTime EventEndDateTime { get; set; }
        public int SeatLimit { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public Repeats Repeats { get; set; }

        //extra logic for the viewmodel
        //public int BookingsMade => GetBookingsMadeForEvent();
        public int BookingsMade { get; set; }
      
        public bool IsFull => BookingsMade == SeatLimit;
    }
}
