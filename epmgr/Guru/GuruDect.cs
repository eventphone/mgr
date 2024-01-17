using Newtonsoft.Json;

namespace epmgr.Guru
{
    public class GuruDect
    {
        [JsonProperty("ipei")]
        public string Ipei { get; set; }

        [JsonProperty("uak")]
        public string Uak { get; set; }
    }
}