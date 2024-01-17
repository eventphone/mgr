using epmgr.Data;
using Newtonsoft.Json;

namespace epmgr.Guru
{
    //{"number": "4502", "type": "DECT", "displayModus": "NUMBER_NAME", "token": "67789479", "location": "", "useEncryption": false, "announcement_lang":"en-GB"}
    public class GuruData
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

        public string Name { get; set; }

        public string Number { get; set; }

        [JsonProperty("type")]
        private MgrExtensionType? _guruType;

        [JsonIgnore]
        public MgrExtensionType Type
        {
            get
            {
                switch (_guruType)
                {
                    case MgrExtensionType.SIP:
                        if (_isPremium.GetValueOrDefault())
                            return MgrExtensionType.PREMIUM;
                        return MgrExtensionType.SIP;
                    default:
                        return _guruType??MgrExtensionType.SPECIAL;
                }
            }
            set
            {
                _guruType = value;
                if (Type == MgrExtensionType.PREMIUM)
                {
                    _guruType = MgrExtensionType.SIP;
                    _isPremium = true;
                }
                else
                {
                    _isPremium = false;
                }
            }
        }

        [JsonProperty("displayModus")]
        public DectDisplayModus? DisplayModus { get; set; }

        public string Token { get; set; }
        
        public bool? UseEncryption { get; set; }

        public string Password { get; set; }

        [JsonProperty("isPremium")]
        private bool? _isPremium;
        
        public string Location { get; set; }

        [JsonProperty("extensions")]
        public GuruExtension[] Extensions { get; set; }

        [JsonProperty("announcement_lang")]
        public string Language { get; set; }

        [JsonProperty("ringback_tone")]
        public string RingbackTone { get; set; }

        [JsonProperty("use3G")]
        public bool? Use3G { get; set; }

        [JsonProperty("ipei")]
        public string Ipei { get; set; }

        [JsonProperty("uak")]
        public string Uak { get; set; }

        [JsonProperty("extension")]
        public string Extension { get; set; }

        [JsonProperty("dect")]
        public GuruDect Dect { get; set; }

        [JsonProperty("allow_dialout")]
        public bool? AllowDialout { get; set; }

        [JsonProperty("call_waiting")]
        public bool? CallWaiting { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("forward_mode")]
        public string ForwardingMode { get; set; }

        [JsonProperty("forward_extension")]
        public string ForwardingExtension { get; set; }

        [JsonProperty("forward_delay")]
        public int? ForwardingDelay { get; set; }

        [JsonProperty("has_multiring")]
        public bool? HasMultiring { get; set; }

        [JsonProperty("old_extension")]
        public string OldExtension { get; set; }

        [JsonProperty("new_extension")]
        public string NewExtension { get; set; }

        [JsonProperty("trunk")]
        public bool? IsTrunk { get; set; }

        [JsonProperty("announcement_audio")]
        public string AnnouncementAudio { get; set; }

        [JsonProperty("direct_routing_target")]
        public string DirectRoutingTarget { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, _settings);
        }
    }

    public class GuruExtension
    {
        [JsonProperty("extension")]
        public string Extension { get; set; }

        [JsonProperty("delay")]
        public int? Delay { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}
