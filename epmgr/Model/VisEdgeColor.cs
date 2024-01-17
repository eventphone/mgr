using Newtonsoft.Json;
using System;
using System.Drawing;

namespace epmgr.Model
{
    public class VisEdgeColor
    {
        public VisEdgeColor(Color color)
        {
            Color = color;
            Highlight = color;
            Hover = color;
        }

        [JsonProperty("color")]
        [JsonConverter(typeof(ColorHtmlConverter))]
        public Color Color { get; set; }

        [JsonProperty("highlight")]
        [JsonConverter(typeof(ColorHtmlConverter))]
        public Color Highlight { get; set; }

        [JsonProperty("hover")]
        [JsonConverter(typeof(ColorHtmlConverter))]
        public Color Hover { get; set; }
    }

    public class ColorHtmlConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color)value;
            writer.WriteValue($"#{color.R.ToString("X2")}{color.G.ToString("X2")}{color.B.ToString("X2")}");
        }
    }
}
