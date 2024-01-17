using Newtonsoft.Json;
using System;
using System.Drawing;

namespace epmgr.Model
{
    public class VisEdge
    {
        public VisEdge()
        {
            Color = new VisEdgeColor(new Color());
        }
        /// <summary>
        /// To draw an arrow with default settings a string can be supplied. For example: <code>arrows:'to, from,
        /// middle'</code> or <code>'to;from'</code>, any combination with any seperating symbol is fine. If you
        /// want to control the size of the arrowheads, you can supply an object.
        /// </summary>
        [JsonProperty("arrows")]
        public string Arrows { get; set; }

        /// <summary>
        /// The color object contains the color information of the edge in every situation. When the edge only needs
        /// a single color, a color value like
        /// </summary>
        [JsonProperty("color")]
        public VisEdgeColor Color { get; set; }

        /// <summary>
        /// When true, the edge will be drawn as a dashed line. You can customize the dashes
        /// by supplying an Array. Array formart: Array of numbers, gap length, dash length,
        /// gap length, dash length, ... etc. The array is repeated until the distance is filled.
        /// When using dashed lines in IE versions older than 11, the line will be drawn straight,
        /// not smooth.
        /// </summary>
        [JsonProperty("dashes")]
        public bool Dashes { get; set; }

        /// <summary>
        /// Edges are between two nodes, one to and one from. This is where you define the
        /// from node. You have to supply the corresponding node ID. This naturally only
        /// applies to individual edges.
        /// </summary>
        [JsonProperty("from")]
        public string From { get; set; }

        /// <summary>
        /// The id of the edge. The id is optional for edges. When not supplied, 
        /// an UUID will be assigned to the edge. This naturally only applies to individual edges.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The label of the edge. HTML does not work in here because the network uses HTML5 Canvas.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// Determines whether or not the label becomes bold when the edge is selected.
        /// </summary>
        [JsonProperty("labelHighlightBold")]
        public bool LabelHighlightBold { get; set; }

        /// <summary>
        /// The title is shown in a pop-up when the mouse moves over the edge.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Edges are between two nodes, one to and one from. This is where you define the
        /// to node. You have to supply the corresponding node ID. This naturally only applies
        /// to individual edges.
        /// </summary>
        [JsonProperty("to")]
        public string To { get; set; }

        /// <summary>
        /// When a value is set, the edges' width will be scaled using the options in the scaling
        /// object defined above.
        /// </summary>
        //[JsonProperty("value")]
        //public int? Value { get; set; }

    }
}
