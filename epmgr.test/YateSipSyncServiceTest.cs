using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class YateSipSyncServiceTest : YateSyncServiceTest<YateSipSyncService, SipUser>
    {
        private DbContextOptions<SipDbContext> _sipDbOptions;

        protected override YateSipSyncService GetNullService()
        {
            return new YateSipSyncService(null, null);
        }

        protected override YateSipSyncService GetService([CallerMemberName] string caller = "")
        {
            _sipDbOptions = new DbContextOptionsBuilder<SipDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new YateSipSyncService(NullLogger<YateSipSyncService>.Instance, new SipDbContext(_sipDbOptions));
        }

        protected override void AddUser(YateUser user)
        {
            var sipUser = new SipUser
            {
                Type = user.Type,
                Username = user.Username,
                DisplayName = user.DisplayName,
                InUse = user.InUse,
                Password = user.Password
            };
            using (var context = new SipDbContext(_sipDbOptions))
            {
                context.Users.Add(sipUser);
                context.SaveChanges();
            }
        }

        protected override IEnumerable<SipUser> GetUsers()
        {
            using (var context = new SipDbContext(_sipDbOptions))
            {
                return context.Users.ToList();
            }
        }

        protected override IEnumerable<string> GetNonMatchingUpdateExtensions()
        {
            yield return GuruTestData.UpdateApp;
            yield return GuruTestData.UpdatePremium;
            yield return GuruTestData.UpdateDect;
            yield return GuruTestData.UpdateGsm;
        }

        protected override IEnumerable<string> GetNonMatchingCreateExtensions()
        {
            return GetNonMatchingUpdateExtensions();
        }

        protected override GuruData GetMatchingUpdateExtension()
        {
            return JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateSip);
        }

        protected override GuruData GetMatchingCreateExtension()
        {
            return GetMatchingUpdateExtension();
        }

        protected override GuruData GetMatchingCreateTrunk()
        {
            return JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"announcement_lang\": \"de-DE\",  \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"SIP\", \"password\": \"PRQ0Y8lX\", \"isPremium\": false}");
        }

        protected override void ValidateUser(SipUser user)
        {
            Assert.Equal("user", user.Type);
            Assert.Equal("iA7y0AwK", user.Password);
        }
    }
}