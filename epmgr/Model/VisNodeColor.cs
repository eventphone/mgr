using Newtonsoft.Json;
using System.Drawing;

namespace epmgr.Model
{
    public class VisNodeColor: VisColor
    {
        [JsonProperty("highlight")] 
        public VisColor Highlight { get; set; }

        [JsonProperty("hover")]
        public VisColor Hover { get; set; }

    }

    public class VisColor
    {
        [JsonProperty("border")]
        public Color Border { get; set; }

        [JsonProperty("background")]
        public Color Background { get; set; }
    }
}
