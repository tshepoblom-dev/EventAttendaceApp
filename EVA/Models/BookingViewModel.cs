using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Models
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int EventId { get; set; }
        public virtual Event Event { get; set; }
        public byte[] UserProfilePic { get; set; }
        public string UserProfilePic_ { get; set; }
        public Status Status { get; set; }
    }
}
