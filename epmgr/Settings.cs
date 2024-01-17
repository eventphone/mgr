using System;

namespace epmgr
{
    public class Settings
    {
        public Settings()
        {
            AllowedExtensions = Array.Empty<string>();
        }

        public static readonly string DectTempPrefix = "9956";

        public string DefaultLanguage { get; set; }

        public string GraphiteHost { get; set; }

        public int GraphitePort { get; set; } = 2003;

        public string[] AllowedExtensions { get; set; }

        public string ApiKey { get; set; }

        public string YateUrl { get; set; }
    }

    public class OmmSettings
    {
        public OmmSettings()
        {
            IPEIBlacklist = Array.Empty<string>();
        }

        public string Hostname { get; set; }

        public int Port { get; set; } = 12622;

        public string User { get; set; }

        public string Password { get; set; }

        public string[] IPEIBlacklist { get; set; }

    }

    public class GuruSettings
    {
        public string Endpoint { get; set; }

        public string ApiKey { get; set; }

        public string EventId { get; set; }

        public int Interval { get; set; }

        public string RingbackFolder { get; set; }

        public string AnnouncementFolder { get; set; }
    }

    public class GsmSettings
    {
        public string Endpoint { get; set; }

        public string SipServer { get; set; }
    }

    public class YwsdSettings
    {
        public string AppEndpoint { get; set; }
        public string DectEndpoint { get; set; }
        public string GsmEndpoint { get; set; }
        public string PremiumEndpoint { get; set; }
        public string SipEndpoint { get; set; }
    }

    public class MapSettings
    {
        public double North { get; set; }

        public double East { get; set; }

        public double South { get; set; }

        public double West { get; set; }

        public MapTilesSetting[] Tiles { get; set; } = Array.Empty<MapTilesSetting>();

        public double LongitudeEpsilon => (East - West) / UInt16.MaxValue;

        public double LatitudeEpsilon => (North - South) / UInt16.MaxValue;

        public MapLevelSetting[] Levels { get; set; } = Array.Empty<MapLevelSetting>();

        public bool SimpleCRS { get; set; }

        public double GetLatitude(int y)
        {
            return North - y * LatitudeEpsilon;
        }

        public double GetLongitude(int x)
        {
            return West + x * LongitudeEpsilon;
        }
    }

    public class MapTilesSetting
    {
        public string Url { get; set; }

        public byte MinZoom { get; set; } = 0;

        public byte MaxZoom { get; set; } = 18;

        public double[][] Bounds { get; set; }
    }

    public class MapLevelSetting
    {
        public string Name { get; set; }

        public string TilesUrl { get; set; }

        public byte MinZoom { get; set; } = 0;

        public byte MaxZoom { get; set; } = 18;

        public double[][] Bounds { get; set; }
    }
}
