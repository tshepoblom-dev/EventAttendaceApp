using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EVA.Data;
using Microsoft.EntityFrameworkCore;

namespace EVA.Models
{
    public class Event
    {

        [Key]
        public int EventId { get; set; }
        public string Title { get; set; }
        public DateTime EventStartDateTime { get; set; }
        public DateTime EventEndDateTime { get; set; }
        public int SeatLimit { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }        
        public Repeats Repeats { get; set; }
    }
}
