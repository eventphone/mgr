using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace epmgr.Model
{
    public class VisData
    {
        public VisData()
        {
            Nodes = new List<VisNode>();
            Edges = new List<VisEdge>();
        }

        [JsonProperty(PropertyName = "nodes")]
        public List<VisNode> Nodes { get; set; }

        [JsonProperty(PropertyName = "edges")]
        public List<VisEdge> Edges { get; set; }
    }
}
