using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace epmgr.Model
{
    public enum VisShapeType
    {
        /// <summary>
        /// Ellipse, label inside
        /// </summary>
        [EnumMember(Value = "ellipse")]
        Ellipse,
        /// <summary>
        /// Circle, label inside
        /// </summary>
        [EnumMember(Value = "circle")]
        Circle,
        /// <summary>
        /// Database, label inside
        /// </summary>
        [EnumMember(Value = "database")]
        Database,
        /// <summary>
        /// Box, label inside
        /// </summary>
        [EnumMember(Value = "box")]
        Box,
        /// <summary>
        /// Text, label inside
        /// </summary>
        [EnumMember(Value = "text")]
        Text,
        /// <summary>
        /// Image, label outside
        /// </summary>
        [EnumMember(Value = "image")]
        Image,
        /// <summary>
        /// CircularImage, label outside
        /// </summary>
        [EnumMember(Value = "circularImage")]
        CircularImage,
        /// <summary>
        /// Diamond, label outside
        /// </summary>
        [EnumMember(Value = "diamond")]
        Diamond,
        /// <summary>
        /// Dot, label outside
        /// </summary>
        [EnumMember(Value = "dot")]
        Dot,
        /// <summary>
        /// Star, label outside
        /// </summary>
        [EnumMember(Value = "star")]
        Star,
        /// <summary>
        /// Triangle, label outside
        /// </summary>
        [EnumMember(Value = "triangle")]
        Triangle,
        /// <summary>
        /// TrianbleDown, label outside
        /// </summary>
        [EnumMember(Value = "triangleDown")]
        TriangleDown,
        /// <summary>
        /// Hexagon, label outside
        /// </summary>
        [EnumMember(Value = "hexagon")]
        Hexagon,
        /// <summary>
        /// Square, label outside
        /// </summary>
        [EnumMember(Value = "square")]
        Square,
        /// <summary>
        /// Icon, label outside
        /// </summary>
        [EnumMember(Value = "icon")]
        Icon
    }

    public class VisNode
    {
        public VisNode()
        {
            Value = 0;
            BorderWidth = 1;
            BorderWidthSelected = 2;
            Size = 26;
            Color = new VisNodeColor
            {
                Highlight = new VisColor(),
                Hover = new VisColor()
            };
            Physics = true;
        }
        /// <summary>
        /// The width of the border of the node.
        /// </summary>
        [JsonProperty("borderWidth")]
        public int BorderWidth { get; set; }

        /// <summary>
        /// The width of the border of the node when it is selected. When undefined, the 
        /// borderWidth * 2 is used.
        /// </summary>
        [JsonProperty("borderWidthSelected")]
        public int BorderWidthSelected { get; set; }

        /// <summary>
        /// When the shape is set to image or circularImage, this option can be an URL to a 
        /// backup image in case the URL supplied in the image option cannot be resolved.
        /// </summary>
        [JsonProperty("brokenImage")]
        public string BrokenImage { get; set; }

        /// <summary>
        /// The color object contains the color information of the node in every situation. 
        /// When the node only needs a single color, a color value like
        /// </summary>
        [JsonProperty("color")]
        public VisNodeColor Color { get; set; }

        /// <summary>
        /// When not undefined, the node will belong to the defined group. Styling information
        /// of that group will apply to this node. Node specific styling overrides group styling.
        /// </summary>
        [JsonProperty(PropertyName = "group", NullValueHandling = NullValueHandling.Ignore)]
        public string Group { get; set; }

        /// <summary>
        /// The id of the node. The id is mandatory for nodes and they have to be unique.
        /// This should obviously be set per node, not globally.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The label is the piece of text shown in or under the node, depending on the shape.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// Determines whether or not the label becomes bold when the node is selected.
        /// </summary>
        [JsonProperty("labelHighlightBold")]
        public bool LabelHighlightBold { get; set; }

        /// <summary>
        /// When false, the node is not part of the physics simulation. It will not 
        /// move except for from manual dragging.
        /// </summary>
        [JsonProperty("physics")]
        public bool Physics { get; set; }

        /// <summary>
        /// The shape defines what the node looks like. There are two types of nodes. One
        /// type has the label inside of it and the other type has the label underneath it.
        /// </summary>
        [JsonProperty("shape")]
        [JsonConverter(typeof(StringEnumConverter))]
        public VisShapeType Shape { get; set; }

        /// <summary>
        /// The size is used to determine the size of node shapes that do not have the label
        /// inside of them. These shapes are: image, circularImage, diamond, dot, star,
        /// triangle, triangleDown, hexagon, square and icon
        /// </summary>
        [JsonProperty("size")]
        public int Size { get; set; }

        /// <summary>
        /// Title to be displayed when the user hovers over the node. The title can be an HTML 
        /// element or a string containing plain text or HTML.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// When a value is set, the nodes will be scaled using the options in the scaling object
        /// defined above.
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; }

        /// <summary>
        /// This gives a node an initial x position. When using the hierarchical layout, 
        /// either the x or y position is set by the layout engine depending on the type 
        /// of view. The other value remains untouched. When using stabilization, the stabilized 
        /// position may be different from the initial one. To lock the node to that position 
        /// use the physics or fixed options.
        /// </summary>
        [JsonProperty("x")]
        public int X { get; set; }

        /// <summary>
        /// This gives a node an initial y position. When using the hierarchical layout, either
        /// the x or y position is set by the layout engine depending on the type of view. The
        /// other value remains untouched. When using stabilization, the stabilized position may
        /// be different from the initial one. To lock the node to that position use the physics
        /// or fixed options.
        /// </summary>
        [JsonProperty("y")]
        public int Y { get; set; }
        
    }
}
