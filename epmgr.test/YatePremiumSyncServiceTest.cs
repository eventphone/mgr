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
    public class YatePremiumSyncServiceTest : YateSyncServiceTest<YatePremiumSyncService, PremiumUser>
    {
        private DbContextOptions<PremiumDbContext> _premiumDbOptions;

        protected override YatePremiumSyncService GetNullService()
        {
            return new YatePremiumSyncService(null, null);
        }

        protected override YatePremiumSyncService GetService([CallerMemberName] string caller = "")
        {
            _premiumDbOptions = new DbContextOptionsBuilder<PremiumDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new YatePremiumSyncService(NullLogger<YatePremiumSyncService>.Instance, new PremiumDbContext(_premiumDbOptions));
        }

        protected override void AddUser(YateUser user)
        {
            var premiumUser = new PremiumUser
            {
                Type = user.Type,
                Username = user.Username,
                DisplayName = user.DisplayName,
                InUse = user.InUse,
                Password = user.Password
            };
            using (var context = new PremiumDbContext(_premiumDbOptions))
            {
                context.Users.Add(premiumUser);
                context.SaveChanges();
            }
        }

        protected override IEnumerable<PremiumUser> GetUsers()
        {
            using (var context = new PremiumDbContext(_premiumDbOptions))
            {
                return context.Users.ToList();
            }
        }

        protected override IEnumerable<string> GetNonMatchingUpdateExtensions()
        {
            yield return GuruTestData.UpdateApp;
            yield return GuruTestData.UpdateSip;
            yield return GuruTestData.UpdateDect;
            yield return GuruTestData.UpdateGsm;
        }

        protected override IEnumerable<string> GetNonMatchingCreateExtensions()
        {
            return GetNonMatchingUpdateExtensions();
        }

        protected override GuruData GetMatchingUpdateExtension()
        {
            return JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdatePremium);
        }

        protected override GuruData GetMatchingCreateExtension()
        {
            return GetMatchingUpdateExtension();
        }

        protected override GuruData GetMatchingCreateTrunk()
        {
            return JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"announcement_lang\": \"de-DE\",  \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"SIP\", \"password\": \"PRQ0Y8lX\", \"isPremium\": true}");
        }

        protected override void ValidateUser(PremiumUser user)
        {
            Assert.Equal("user", user.Type);
            Assert.Equal("40CD1777-052A-4056-93BE-4EB7F633FA25", user.Password);
        }
    }
}