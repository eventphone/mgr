using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class YateGsmSyncServiceTest : YateSyncServiceTest<YateGsmSyncService, GsmUser>
    {
        private DbContextOptions<GsmDbContext> _gsmDbOptions;

        protected override YateGsmSyncService GetNullService()
        {
            return new YateGsmSyncService(null, null, null);
        }

        protected override YateGsmSyncService GetService([CallerMemberName] string caller = "")
        {
            _gsmDbOptions = new DbContextOptionsBuilder<GsmDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new YateGsmSyncService(NullLogger<YateGsmSyncService>.Instance, new GsmDbContext(_gsmDbOptions), new OptionsWrapper<GsmSettings>(new GsmSettings{SipServer = "gsmserver"}));
        }

        protected override void AddUser(YateUser user)
        {
            var gsmUser = new GsmUser
            {
                Type = user.Type,
                Username = user.Username,
                DisplayName = user.DisplayName,
                InUse = user.InUse,
                Password = user.Password
            };
            using (var context = new GsmDbContext(_gsmDbOptions))
            {
                context.Users.Add(gsmUser);
                context.SaveChanges();
            }
        }

        protected override IEnumerable<GsmUser> GetUsers()
        {
            using (var context = new GsmDbContext(_gsmDbOptions))
            {
                return context.Users.ToList();
            }
        }

        protected override IEnumerable<string> GetNonMatchingUpdateExtensions()
        {
            yield return GuruTestData.UpdateApp;
            yield return GuruTestData.UpdateSip;
            yield return GuruTestData.UpdatePremium;
            yield return GuruTestData.UpdateDect;
        }

        protected override IEnumerable<string> GetNonMatchingCreateExtensions()
        {
            return GetNonMatchingUpdateExtensions();
        }

        protected override GuruData GetMatchingUpdateExtension()
        {
            var result = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateGsm);
            result.Password = "asdf";
            return result;
        }

        protected override GuruData GetMatchingCreateExtension()
        {
            return null;
        }

        protected override GuruData GetMatchingCreateTrunk()
        {
            return JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"announcement_lang\": \"de-DE\",  \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"GSM\", \"password\": \"PRQ0Y8lX\", \"isPremium\": false}");
        }

        protected override void ValidateUser(GsmUser user)
        {
            Assert.Equal("static", user.Type);
            Assert.Equal("asdf", user.Password);
            Assert.Contains("gsmserver", user.StaticTarget);
        }
    }
}