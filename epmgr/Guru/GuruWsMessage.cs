using Newtonsoft.Json;

namespace epmgr.Guru
{
    public class GuruWsMessage
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        
        [JsonProperty("queuelength")]
        public int Length { get; set; }
    }
}