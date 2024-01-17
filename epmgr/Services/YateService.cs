using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace epmgr.Services
{
    public class YateService:IDisposable
    {
        private readonly HttpClient _client;

        public YateService(IOptions<Settings> options)
        {
            _client = new HttpClient { BaseAddress = new Uri(options.Value.YateUrl) };
        }

        public async Task PlayWave(string filename, string extension, string caller, string callername, CancellationToken cancellationToken)
        {
            var message = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("target", extension),
                new KeyValuePair<string, string>("caller", caller),
                new KeyValuePair<string, string>("callername", callername),
                new KeyValuePair<string, string>("soundfile", filename),
                new KeyValuePair<string, string>("delay", "2"),
                new KeyValuePair<string, string>("max_ringtime", "50"),
            });
            using (var response = await _client.PostAsync("call", message, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        #region IDisposable Support
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
