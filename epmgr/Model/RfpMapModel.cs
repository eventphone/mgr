namespace epmgr.Model
{
    public class RfpMapModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double Latitude { get; set; }
        
        public double Longitude { get; set; }

        public bool IsOutdoor { get; set; }

        public string State { get; set; }

        public string Mac { get; set; }

        public string Ip { get; set; }

        public string Type { get; set; }

        public string Level { get; set; }

        public bool IsPositioned { get; set; }
    }

    public class RfpSyncMapModel
    {
        public int From { get; set; }

        public int To { get; set; }

        public int Rssi { get; set; }

        public int Offset { get; set; }

        public bool Lost { get; set; }
    }
}