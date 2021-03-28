using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Models
{
    public class SMS
    {
        public int SmsId { get; set; }
        public string ToUserId { get; set; }
        public string Content { get; set; }
        public DateTime TimeSent { get; set; }
        public bool SuccessfullySent { get; set; }
    }
}
