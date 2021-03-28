using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Services
{
    public interface ISMSSender
    {
        Task<bool> SendSmsAsync(string message, string to, string channel = "sms");
    }
}
