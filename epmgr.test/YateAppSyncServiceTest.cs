using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class YateAppSyncServiceTest : YateSyncServiceTest<YateAppSyncService, AppUser>
    {
        private DbContextOptions<AppDbContext> _appDbOptions;
        
        protected override YateAppSyncService GetNullService()
        {
            return new YateAppSyncService(null, null, null);
        }

        protected override YateAppSyncService GetService([CallerMemberName] string caller = "")
        {
            _appDbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            var options = new OptionsWrapper<GuruSettings>(new GuruSettings{AnnouncementFolder = "/opt/sounds/announcements"});
            return new YateAppSyncService(NullLogger<YateAppSyncService>.Instance, new AppDbContext(_appDbOptions), options);
        }

        protected override void AddUser(YateUser user)
        {
            var appUser = new AppUser
            {
                Type = user.Type,
                Username = user.Username,
                DisplayName = user.DisplayName,
                InUse = user.InUse,
                Password = user.Password
            };
            using (var context = new AppDbContext(_appDbOptions))
            {
                context.Users.Add(appUser);
                context.SaveChanges();
            }
        }

        protected override IEnumerable<AppUser> GetUsers()
        {
            using (var context = new AppDbContext(_appDbOptions))
            {
                return context.Users.ToList();
            }
        }

        protected override IEnumerable<string> GetNonMatchingUpdateExtensions()
        {
            yield return GuruTestData.UpdateSip;
            yield return GuruTestData.UpdatePremium;
            yield return GuruTestData.UpdateDect;
            yield return GuruTestData.UpdateGsm;
            yield return GuruTestData.UpdateApp;
        }

        protected override IEnumerable<string> GetNonMatchingCreateExtensions()
        {
            return GetNonMatchingUpdateExtensions();
        }

        protected override GuruData GetMatchingUpdateExtension()
        {
            return JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateAppDirectTarget);
        }

        protected override GuruData GetMatchingCreateExtension()
        {
            return GetMatchingUpdateExtension();
        }

        protected override GuruData GetMatchingCreateTrunk()
        {
            return JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"announcement_lang\": \"de-DE\",  \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"ANNOUNCEMENT\", \"password\": \"PRQ0Y8lX\", \"isPremium\": false}");
        }

        protected override void ValidateUser(AppUser user)
        {
            Assert.Equal("static", user.Type);
            Assert.False(String.IsNullOrEmpty(user.StaticTarget));
        }

        [Fact]
        public async Task UpdateExtensionGeneratesEmptyPassword()
        {
            var service = GetService();

            var guruData = new GuruMessage {Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateAnnouncement)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            var user = Assert.Single(GetUsers());
            Assert.Empty(user.Password);
        }
    }
}