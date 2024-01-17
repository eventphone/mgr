using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Services;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public static class GuruTestData
    {
        public static readonly string UpdateSpecial ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"SPECIAL\"}";
        public static readonly string UpdateApp ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"APP\"}";
        public static readonly string UpdateAppDirectTarget ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"APP\", \"direct_routing_target\":\"nirvana\"}";
        public static readonly string UpdateAnnouncement ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"ANNOUNCEMENT\"}";
        public static readonly string UpdateSip ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"SIP\", \"password\": \"iA7y0AwK\", \"isPremium\": false}";
        public static readonly string UpdatePremium ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"SIP\", \"password\": \"40CD1777-052A-4056-93BE-4EB7F633FA25\", \"isPremium\": true}";
        public static readonly string UpdateDect ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"DECT\", \"token\": \"49387470\", \"useEncryption\": false, \"displayModus\": \"NUMBER_NAME\"}";
        public static readonly string UpdateGsm ="{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"GSM\", \"token\": \"71902311\", \"use2G\": true, \"use3G\": true, \"use4G\": false}";
    }

    public abstract class YateSyncServiceTest<T,TUser> where T:YateSyncService<TUser> where TUser : YateUser, new()
    {
        protected abstract T GetNullService();
        protected abstract T GetService([CallerMemberName] string caller = "");

        private void AddUser(string username)
        {
            AddUser(new AppUser{Username = username});
        }

        protected abstract void AddUser(YateUser user);
        protected abstract IEnumerable<TUser> GetUsers();
        protected abstract IEnumerable<string> GetNonMatchingUpdateExtensions();
        protected abstract IEnumerable<string> GetNonMatchingCreateExtensions();
        protected abstract GuruData GetMatchingUpdateExtension();
        protected abstract GuruData GetMatchingCreateExtension();
        protected abstract GuruData GetMatchingCreateTrunk();
        protected abstract void ValidateUser(TUser user);

        [Fact]
        public async Task StartSyncIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage {Type = "SYNC_STARTED", Data = new GuruData(), Id = 47193};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task EndSyncIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage {Type = "SYNC_ENDED", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task UpdateCallGroupIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP"};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task DeleteExtensionCanBeTriggered()
        {
            var service = GetService();

            IExtensionService extensionService = service;
            await extensionService.DeleteExtension("1234", CancellationToken.None);
        }

        [Fact]
        public async Task DeleteExtensionTriggerRemovesExtension()
        {
            var service = GetService();
            AddUser("2001");

            await service.DeleteExtension("2001", CancellationToken.None);

            Assert.Empty(GetUsers());
        }

        [Fact]
        public async Task DeleteExtensionRemovesYwsdExtension()
        {
            var service = GetService();
            AddUser("2000");
            AddUser("2001");
            
            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            var extension = GetUsers().Single();
            Assert.NotEqual("2001", extension.Username);
        }

        [Fact]
        public async Task DeleteExtensionForNonExistingIsIgnored()
        {
            var service = GetService();
            AddUser("2001");

            var guruData = new GuruMessage { Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"1234\"}") };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            Assert.NotEmpty(GetUsers());
        }

        [Fact]
        public async Task NonMatchingUpdateExtensionsDeletesUser()
        {
            foreach (var update in GetNonMatchingUpdateExtensions())
            {
                await NonMatchingUpdateExtensionDeletesUser(update);
            }
        }

        [Fact]
        public async Task UpdateSpecialIsIgnored()
        {
            var service = GetNullService();
            
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateSpecial)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        private async Task NonMatchingUpdateExtensionDeletesUser(string update)
        {
            var service = GetService(nameof(NonMatchingUpdateExtensionDeletesUser) + update);
            AddUser("4711");

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(update)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            Assert.Empty(GetUsers());
        }

        [Fact]
        public async Task MatchingCreateExtensionAddsUser()
        {
            var service = GetService();
            var update = GetMatchingCreateExtension();
            if (update == null) return;

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = update};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            var user = GetUsers().Single();
            Assert.Equal("4711", user.Username);
            Assert.Matches("(user|static)", user.Type);
            Assert.Equal("Unit Test", user.DisplayName);
            ValidateUser(user);
        }

        [Fact]
        public async Task MatchingUpdateExtensionUpdatesUser()
        {
            var service = GetService();
            AddUser(new AppUser
            {
                Username = "4711",
                DisplayName = "asdf",
                Type = "app"
            });
            var update = GetMatchingUpdateExtension();

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = update};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            var user = GetUsers().Single();
            Assert.Equal("4711", user.Username);
            Assert.Matches("(user|static)", user.Type);
            Assert.Equal("Unit Test", user.DisplayName);
            ValidateUser(user);
        }

        [Fact]
        public async Task NonMatchingCreateExtensionsIsIgnored()
        {
            foreach (var update in GetNonMatchingCreateExtensions())
            {
                await NonMatchingCreateExtensionIsIgnored(update);
            }
        }

        private async Task NonMatchingCreateExtensionIsIgnored(string update)
        {
            var service = GetService(nameof(NonMatchingUpdateExtensionDeletesUser) + update);
            
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(update)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            Assert.Empty(GetUsers());
        }

        [Fact]
        public async Task UpdateTrunkSetTrunkFlag()
        {
            var service = GetService();
            AddUser(new AppUser
            {
                Username = "4711",
                DisplayName = "asdf",
                Type = "app"
            });
            var user = GetUsers().Single();
            Assert.Equal("4711", user.Username);
            Assert.False(user.IsTrunk);

            await service.SetTrunking("4711", true, CancellationToken.None);
            
            user = GetUsers().Single();
            Assert.Equal("4711", user.Username);
            Assert.True(user.IsTrunk);
        }

        [Fact]
        public void ValidateAddUser()
        {
            GetService();
            Assert.Empty(GetUsers());
            AddUser("1234");
            Assert.Single(GetUsers(), x=>x.Username == "1234");
        }

        [Fact]
        public async Task RenameExtension()
        {
            var service = GetService();
            AddUser(new AppUser
            {
                Username = "4503",
                DisplayName = "asdf",
                Type = "app"
            });

            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            var updated = GetUsers().Single();
            Assert.Equal("4502", updated.Username);
            Assert.Equal("asdf", updated.DisplayName);
        }

        [Fact]
        public async Task RenameNonExistingExtension()
        {
            var service = GetService();
            AddUser(new AppUser
            {
                Username = "4511",
                DisplayName = "asdf",
                Type = "app"
            });
            var user = GetUsers().Single();

            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            var updated = GetUsers().Single();
            Assert.Equal("4511", updated.Username);
            Assert.Equal("asdf", updated.DisplayName);
        }

        [Fact]
        public async Task UnsubscribeDeviceIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage {Type = "UNSUBSCRIBE_DEVICE", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task Trunk()
        {
            var service = GetService();

            var update = GetMatchingCreateTrunk();
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = update};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            var user = GetUsers().Single();
            Assert.True(user.IsTrunk);
        }
    }
}