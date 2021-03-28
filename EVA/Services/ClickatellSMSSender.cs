using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EVA.Services
{
    public class ClickatellSMSSender : ISMSSender
    {

        public IConfiguration Configuration { get; }
        public ClickatellSMSSender(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        #region Methods
        /// Send SMS 
        /// </summary>
        /// <param name="message">Text</param>
        /// <param name="to">recipient number</param>
        /// <param name="channel">channel wherewith to send message, either SMS or WhatsApp, default is SMS</param>
        /// <param name="settings">Clickatel config setting</param>
        /// <returns>True if SMS was successfully sent; otherwise false</returns>
        public async Task<bool> SendSmsAsync(string message, string to, string channel = "sms")
        {
            try
            {
                var apiKey = Configuration.GetValue<string>("SMSProvider");
                var client = new RestClient("https://platform.clickatell.com/v1/message")
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", apiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Access-Control-Allow-Origin", "*");

                JArray array = new JArray();
                dynamic msg = new JObject();
                msg.channel = channel;
                msg.content = message;
                msg.to = FormatNumber(to);

                array.Add(msg);

                JObject o = new JObject();
                o["messages"] = array;

                string jsonObj = o.ToString();

                request.AddParameter("messages", jsonObj, "application/json", ParameterType.RequestBody);
                var response = await client.ExecuteAsync(request);

                //_logger.InsertLog(Core.Domain.Logging.LogLevel.Debug, "sms api request", request.ToJson());

                //if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.MultiStatus || response.StatusCode == System.Net.HttpStatusCode.Accepted)
                if (response.IsSuccessful)
                {
                  //  _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "SMS sent to: " + to, message);
                    Debug.WriteLine("SMS sent to: " + to, message);
                    return true;
                }
                else
                {
                //    _logger.Error(response.Content);
                    Debug.WriteLine(response.Content);
                    return false;
                }
            }
            catch (Exception ex)
            {
            //   _logger.Error("Clickatell SMS Error: ", ex);
                Debug.WriteLine("Clickatell SMS Error: ", ex);
                return false;
            }
        }
        public string FormatNumber(string number)
        {
            char[] numArr = number.ToCharArray();
            if (numArr.Length == 10 && numArr[0] == '0')
            {
                number = number.Remove(0, 1);
                number = number.Insert(0, "27");
            }
            return number;
        }
        #endregion

    }
}
