using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Model;
using epmgr.Omm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using mitelapi.Types;

namespace epmgr.Pages.Omm
{
    public class MapModel : PageModel
    {
        private readonly IOmmClient _client;
        private readonly MapSettings _settings;

        public double North => _settings.North;

        public double East => _settings.East;

        public double South => _settings.South;

        public double West => _settings.West;

        public bool SimpleCRS => _settings.SimpleCRS;

        public MapTilesSetting[] Tiles => SimpleCRS ? Array.Empty<MapTilesSetting>() : _settings.Tiles;

        public MapLevelSetting[] Levels
        {
            get
            {
                if (_settings.Levels == null)
                {
                    return Array.Empty<MapLevelSetting>();
                }
                return _settings.Levels;
            }
        }

        public RfpMapModel[] Rfps { get; private set; }

        public RfpSyncMapModel[] RfpSync { get; private set; }

        public MapModel(IOptions<MapSettings> options, IOmmClient client)
        {
            _client = client;
            _settings = options.Value;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var rfps = await _client.GetRFPAllAsync(true, true, cancellationToken);
            var syncDetailTask = GetSyncDetailsAsync(rfps, cancellationToken);
            Rfps = rfps.Select(x => new RfpMapModel
            {
                Id = x.Id.Value,
                Name = x.Name,
                Latitude = _settings.GetLatitude(x.Y.GetValueOrDefault()),
                Longitude = _settings.GetLongitude(x.X.GetValueOrDefault()),
                IsOutdoor = x.IsOutdoor,
                State = x.SyncState?.ToString(),
                Type = x.HwType?.ToString(),
                Mac = x.EthAddr,
                Ip = x.IpAddr,
                Level = x.Hierarchy2,
                IsPositioned = x.Y.GetValueOrDefault() != 0 || x.X.GetValueOrDefault() != 0
            }).ToArray();
            RfpSync = await syncDetailTask;
        }

        private Task<RfpSyncMapModel[]> GetSyncDetailsAsync(ICollection<RFPType> rfps, CancellationToken cancellationToken)
        {
            var tasks = rfps.Select(x => GetSyncDetailsAsync(x, cancellationToken));
            return Task.WhenAll(tasks)
                .ContinueWith(x => { return x.Result.SelectMany(y => y).ToArray(); }, cancellationToken);
        }

        private async Task<RfpSyncMapModel[]> GetSyncDetailsAsync(RFPType rfp, CancellationToken cancellationToken)
        {
            if (rfp.SyncState != RFPSyncStateType.Synced && rfp.SyncState != RFPSyncStateType.Searching)
            {
                return Array.Empty<RfpSyncMapModel>();
            }
            var sync = await _client.GetRFPSyncAsync(rfp.Id.Value, cancellationToken);
            if (sync.Forward == null)
                return Array.Empty<RfpSyncMapModel>();
            return sync.Forward
                .Where(x => !x.Lost)
                .Select(x => new RfpSyncMapModel
                {
                    From = rfp.Id.Value,
                    To = x.Id,
                    Offset = x.Offset,
                    Rssi = -100 + x.Rssi
                })
                .ToArray();
        }

        public async Task<IActionResult> OnPostPositionAsync([FromBody]RfpMapModel data, CancellationToken cancellationToken)
        {
            var rfp = new RFPType
            {
                Id = data.Id,
                X = (int) ((data.Longitude - West) / _settings.LongitudeEpsilon),
                Y = (int) ((North - data.Latitude) / _settings.LatitudeEpsilon),
                Hierarchy2 = data.Level,
            };
            if (rfp.Y < 0)
                rfp.Y = 0;
            else if (rfp.Y > UInt16.MaxValue)
                rfp.Y = UInt16.MaxValue;
            if (rfp.X < 0)
                rfp.X = 0;
            else if (rfp.X > UInt16.MaxValue)
                rfp.X = UInt16.MaxValue;
            if (rfp.X == 0 && rfp.Y == 0)
                rfp.Hierarchy2 = String.Empty;
            await _client.SetRFPAsync(rfp, cancellationToken);
            return StatusCode(200);
        }
    }
}