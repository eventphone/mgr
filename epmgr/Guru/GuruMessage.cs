using System;
using Newtonsoft.Json;

namespace epmgr.Guru
{
    //{"type": "UPDATE_EXTENSION", "id": 68, "timestamp": 1520693798.597209, "data": {"number": "4502", "type": "DECT", "displayModus": "NUMBER_NAME", "token": "67789479", "location": "", "useEncryption": false}}
    public class GuruMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }

        [JsonProperty("data")]
        public GuruData Data { get; set; }
    }

    public class GuruAck
    {
        [JsonProperty("messages")]
        public GuruAckMessage[] Messages { get; set; }
    }

    public class GuruAckMessage
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
