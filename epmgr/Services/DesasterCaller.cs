using epmgr.Ywsd;
using Hangfire;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace epmgr.Services
{
    public class DesasterCaller
    {
        private readonly YwsdClient _ywsd;
        private readonly YwsdSettings _settings;

        public DesasterCaller(YwsdClient ywsd, IOptions<YwsdSettings> options)
        {
            _ywsd = ywsd;
            _settings = options.Value;
        }
        public async Task GenerateCallsAsync(string name, string announcement, string target, CancellationToken cancellationToken)
        {
            var result = await _ywsd.GetRoutingTreeAsync(_settings.AppEndpoint, "", target, cancellationToken);
            var extensions = GetLeafs(result.Tree).Distinct().ToList();
            foreach ( var extension in extensions )
            {
                BackgroundJob.Enqueue<YateService>(x=>x.PlayWave(announcement, extension, "999", name, CancellationToken.None));
            }
        }

        private IEnumerable<string> GetLeafs(JObject extension)
        {
            if (extension is null) yield break;
            var extensionType = extension["type"]?.Value<string>();
            if (extensionType == "Type.SIMPLE" || extensionType == "Type.MULTIRING")
            {
                if (extension["forwarding_mode"].Value<string>() != "ForwardingMode.ENABLED" ||
                    extension["forwarding_delay"].Value<int>() != 0)
                {
                    yield return extension["extension"].Value<string>();
                }
            }
            var forkRanks = extension["fork_ranks"] as JArray;
            if (forkRanks is not null)
            {
                foreach (var rank in forkRanks)
                {
                    var members = rank["members"] as JArray;
                    if (members is null) continue;
                    foreach (var member in members)
                    {
                        var active = member["active"].Value<bool>();
                        if (!active) continue;
                        var memberExtension = member["extension"] as JObject;
                        foreach (var leaf in GetLeafs(memberExtension))
                        {
                            yield return leaf;
                        }
                    }
                }
            }
            if (extension["forwarding_mode"].Value<string>() == "ForwardingMode.ENABLED")
            {
                var forwarding = extension["forwarding_extension"] as JObject;
                foreach (var leaf in GetLeafs(forwarding))
                {
                    yield return leaf;
                }
            }
        }
    }
}
