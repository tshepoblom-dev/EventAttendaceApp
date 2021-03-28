using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Models
{

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey("Id")]
        public string UserId { get; set; }

        [ForeignKey("EventId")]
        public int EventId { get; set; }

        public Status BookingStatus { get; set; }
    }
}
