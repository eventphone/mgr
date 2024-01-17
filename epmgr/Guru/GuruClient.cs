using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Newtonsoft.Json.Linq;

namespace epmgr.Guru
{
    public class GuruClient:IDisposable
    {
        private readonly string _eventId;
        private readonly HttpClient _client;
        private readonly JsonSerializer _serializer;

        public GuruClient(IOptions<GuruSettings> options)
            :this(options.Value.Endpoint, options.Value.ApiKey, options.Value.EventId)
        {
        }
        public GuruClient(string baseAddress, string apikey, string eventId)
        {
            _eventId = eventId;
            if (String.IsNullOrEmpty(baseAddress)) return;
            _client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            _client.DefaultRequestHeaders.Add("apikey", apikey);
            _serializer = new JsonSerializer{NullValueHandling = NullValueHandling.Ignore};
        }
        
        public async Task<GuruMessage[]> GetMessagesAsync(CancellationToken cancellationToken)
        {
            var response = await _client.GetAsync($"api/event/{_eventId}/messages?max_messages=50", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                return _serializer.Deserialize<GuruMessage[]>(reader);
            }
        }

        public async Task AcknowledgeMessageAsync(GuruMessage message, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("acklist", $"[{message.Id}]") });
            var response = await _client.PostAsync($"api/event/{_eventId}/messages", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<GuruAck> SubmitMessageAsync(IList<MgrMessage> messages, CancellationToken cancellationToken)
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, messages.Select(x=>new { type=x.Type, id=x.Id, timestamp=x.Timestamp, data=JObject.Parse(x.Json)}).ToArray());
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("messages", writer.ToString()),
                    new KeyValuePair<string, string>("acklist", "[]")
                });
                var response = await _client.PostAsync($"api/event/{_eventId}/messages", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsAsync<GuruAck>(cancellationToken);
                return result;
            }
        }

        public Task DownloadRingbackAsync(string ringback, string folder, CancellationToken cancellationToken)
        {
            var target = Path.Combine(folder, ringback + ".slin");
            return DownloadAudioAsync($"api/audio/fetch/ringback/{ringback}", target, cancellationToken);
        }

        public Task DownloadAnnouncementAsync(string announcement, string folder, CancellationToken cancellationToken)
        {
            var target = Path.Combine(folder, announcement + ".slin");
            return DownloadAudioAsync($"api/audio/fetch/plain/{announcement}", target, cancellationToken);
        }

        private async Task DownloadAudioAsync(string url, string target, CancellationToken cancellationToken)
        {
            if (File.Exists(target)) return;
            var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return;
            response.EnsureSuccessStatusCode();
            await using (var file = File.OpenWrite(target))
            {
                await response.Content.CopyToAsync(file, cancellationToken);
            }
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
