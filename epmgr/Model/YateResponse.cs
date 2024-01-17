using Newtonsoft.Json;

namespace epmgr.Model
{
    public class YateResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
