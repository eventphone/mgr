using StackExchange.Redis;

namespace epmgr.Model
{
    public class YateMessage
    {
        public YateMessage(string id, string host, HashEntry[] values)
        {
            Id = id;
            Host = host;
            Level = "secondary";

            foreach (var value in values)
            {
                switch (value.Name)
                {
                    case "level":
                        switch (value.Value)
                        {
                            case "info":
                                Level = "info";
                                break;
                            case "warning":
                                Level = "warning";
                                break;
                            case "error":
                                Level = "danger";
                                break;
                        }
                        break;
                    case "msg":
                        Message = value.Value;
                        break;
                }
            }
        }

        public string Id { get; }

        public string Host { get; }

        public string Message { get; }

        public string Level { get; }
    }
}