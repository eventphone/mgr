using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace epmgr.Gsm
{
    public interface IGsmClient
    {
        Task<SubscriberInfo> GetSubscriberAsync(string extension, CancellationToken cancellationToken);

        Task SetUmtsEnabledAsync(string extension, bool enabled, CancellationToken cancellationToken);

        Task ResetExtensionAsync(string extension, CancellationToken cancellationToken);

        Task UpdateExtensionAsync(string extension, string name, string newExtension, CancellationToken cancellationToken);

        Task<SubscriberInfo[]> GetSubscribersAsync(CancellationToken cancellationToken);

        Task CreateSubscriber(string imsi, string name, CancellationToken cancellationToken);
    }
    public class GsmClient:IGsmClient,IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializer _serializer;

        public GsmClient(IOptions<GsmSettings> settings)
            : this(settings.Value.Endpoint)
        {
        }

        public GsmClient(string endpoint)
        {
            _client = new HttpClient {BaseAddress = new Uri(endpoint)};
            _serializer = new JsonSerializer();
        }

        public async Task PingAsync(CancellationToken cancellationToken)
        {
            var response = await _client.GetAsync("/", cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task CreateSubscriber(string imsi, string name, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                imsi = imsi,
                name = name,
            }), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/create_subscriber", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateExtensionAsync(string extension, string name, string newExtension, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                current_number = extension,
                new_number = newExtension,
                name = name
            }), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/update_number", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task ResetExtensionAsync(string extension, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                number=extension, 
            }), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/release_number", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<SubscriberInfo> GetSubscriberAsync(string extension, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                number=extension, 
            }), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/get_subscriber", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var info = _serializer.Deserialize<SubscriberInfo>(reader);
                if (info.IMSI == null)
                    return null;
                return info;
            }
        }

        public async Task<SubscriberInfo[]> GetSubscribersAsync(CancellationToken cancellationToken)
        {
            var response = await _client.GetAsync("/get_current_subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                return _serializer.Deserialize<SubscriberInfo[]>(reader);
            }
        }

        public async Task SetUmtsEnabledAsync(string extension, bool enabled, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                number = extension,
                rat = "3G",
                flag=enabled?"allowed":"forbidden"
            }), Encoding.UTF8, "application/json");
            var respone = await _client.PostAsync("/update_rat", content, cancellationToken);
            respone.EnsureSuccessStatusCode();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
