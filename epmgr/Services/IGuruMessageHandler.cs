using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Data.ywsd;
using epmgr.Guru;

namespace epmgr.Services
{
    public class MgrCreateExtension
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string Language { get; set; }
        public MgrExtensionType Type { get; set; }
        public string RingbackTone { get; set; }
        public bool Encryption { get; set; }
        public string Password { get; set; }
        public DectDisplayModus DisplayModus { get; set; }
        public GuruDect DectDevice { get; set; }
        public bool AllowDialout { get; set; }
        public bool CallWaiting { get; set; }
        public string ShortName { get; set; }
        public string ForwardingExtension { get; set; }
        public ForwardingMode ForwardingMode { get; set; }
        public int? ForwardingDelay { get; set; }
        public bool HasMultiring { get; set; }
        public bool Use3G { get; set; }
        public bool IsTrunk { get; set; }
        public string AnnouncementAudio { get; set; }
        public string DirectRoutingTarget { get; set; }

        public static MgrCreateExtension Create(GuruData guruData)
        {
            var x = guruData;
            return new MgrCreateExtension
            {
                Name = x.Name,
                Number = x.Number,
                Type = x.Type,
                Language = x.Language,
                RingbackTone = x.RingbackTone,
                Password = x.Password,
                DisplayModus = x.DisplayModus.GetValueOrDefault(),
                Encryption = x.UseEncryption.GetValueOrDefault(),
                DectDevice = x.Dect,
                Use3G = x.Use3G.GetValueOrDefault(),
                AllowDialout = x.AllowDialout.GetValueOrDefault(),
                CallWaiting = x.CallWaiting.GetValueOrDefault(),
                ShortName = x.ShortName,
                ForwardingExtension = x.ForwardingExtension,
                ForwardingDelay = x.ForwardingDelay,
                ForwardingMode = GetForwardingMode(x.ForwardingMode),
                HasMultiring = x.HasMultiring.GetValueOrDefault(),
                IsTrunk = x.IsTrunk.GetValueOrDefault(),
                AnnouncementAudio = x.AnnouncementAudio,
                DirectRoutingTarget = x.DirectRoutingTarget,
            };
        }

        private static ForwardingMode GetForwardingMode(string forwardingMode)
        {
            switch (forwardingMode)
            {
                case null:
                case "":
                case"DISABLED":
                    return ForwardingMode.Disabled;
                case"ENABLED": 
                    return ForwardingMode.Enabled;
                case"ON_BUSY": 
                    return ForwardingMode.OnBusy;
                case"ON_UNAVAILABLE": 
                    return ForwardingMode.OnUnavailable;
                default: throw new ArgumentOutOfRangeException(nameof(forwardingMode));
            }
        }
    }

    public interface IExtensionService
    {
        Task CreateExtension(MgrCreateExtension extension, CancellationToken cancellationToken);

        Task DeleteExtension(string extension, CancellationToken cancellationToken);
    }

    public interface IGuruMessageHandler
    {
        int Order { get; }

        bool IsThreadSafe { get; }

        Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken);
    }
}