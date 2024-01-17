using System;
using Newtonsoft.Json;

namespace epmgr.Gsm
{
    public class SubscriberInfo
    {
        [JsonProperty("ID")]
        public string ID { get; set; }
        
        [JsonProperty("IMSI")]
        public string IMSI { get; set; }

        [JsonProperty("MSISDN")]
        public string Number { get; set; }

        [JsonProperty("last LU seen")]
        public DateTimeOffset LastSeen { get; set; }
    }
}