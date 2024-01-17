using System;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace epmgr.Model
{
    public class YateChannel
    {
        public YateChannel(string id, string host, HashEntry[] values)
        {
            Id = id;
            Host = host;
            Direction = "question-mark";
            foreach (var value in values)
            {
                switch (value.Name)
                {
                    case "ysm_status":
                        YsmStatus = value.Value;
                        break;
                    case "direction":
                        switch (value.Value)
                        {
                            case "incoming":
                                Direction = "account-login";
                                break;
                            case "outgoing":
                                Direction = "account-logout";
                                break;
                        }
                        break;
                    case "status":
                        SipStatus = value.Value;
                        break;
                    case "reason_sip":
                        ReasonSip = value.Value;
                        break;
                    case "caller":
                        Caller = value.Value;
                        break;
                    case "called":
                        Called = value.Value;
                        break;
                    case "address":
                        Address = value.Value;
                        break;
                    case "reason":
                        Reason = value.Value;
                        break;
                    case "cause_sip":
                        CauseSip = value.Value;
                        break;
                }
            }
        }

        public string Id { get; }

        public string Host { get; }

        [JsonIgnore]
        public string YsmStatus { get; }
        
        public string Direction { get; }

        [JsonIgnore]
        public string SipStatus { get; }

        public string Color
        {
            get
            {
                var ystatus = YsmStatus;
                if (ystatus == "hungup" && SipStatus != "answered")
                    ystatus = SipStatus;
                switch (ystatus)
                {
                    case "rejected":
                        return "danger  ";
                    case "ringing":
                        return "warning";
                    case "answered":
                        return "success";
                    case "hungup":
                        return "info";
                    default:
                        return "";
                }
            }
        }

        public string Status
        {
            get
            {
                var result = YsmStatus;
                if (result == "hungup" && SipStatus != "answered")
                    result = SipStatus;
                if (!String.IsNullOrEmpty(Reason))
                    result += ": " + Reason;
                if (!String.IsNullOrEmpty(CauseSip))
                    result += " SIP " + CauseSip + '/' + ReasonSip;
                return result;
            }
        }
        
        [JsonIgnore]
        public string ReasonSip { get; }
        
        public string Caller { get; }
        
        public string Called { get; }

        public string Address { get; }
        
        [JsonIgnore]
        public string Reason { get; }
        
        [JsonIgnore]
        public string CauseSip { get; }
    }
}