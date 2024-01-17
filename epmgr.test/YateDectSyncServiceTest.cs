using System;
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
    public class YateDectSyncServiceTest : YateSyncServiceTest<YateDectSyncService, DectUser>
    {
        private DbContextOptions<DectDbContext> _dectDbOptions;

        protected override YateDectSyncService GetNullService()
        {
            return new YateDectSyncService(null, null);
        }

        protected override YateDectSyncService GetService([CallerMemberName] string caller = "")
        {
            _dectDbOptions = new DbContextOptionsBuilder<DectDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new YateDectSyncService(NullLogger<YateDectSyncService>.Instance, new DectDbContext(_dectDbOptions));
        }

        protected override void AddUser(YateUser user)
        {
            var dectUser = new DectUser
            {
                Type = user.Type,
                Username = user.Username,
                DisplayName = user.DisplayName,
                InUse = user.InUse,
                Password = user.Password,
                DisplayMode = DectDisplayModus.Name,
            };
            using (var context = new DectDbContext(_dectDbOptions))
            {
                context.Users.Add(dectUser);
                context.SaveChanges();
            }
        }

        protected override IEnumerable<DectUser> GetUsers()
        {
            using (var context = new DectDbContext(_dectDbOptions))
            {
                return context.Users.ToList();
            }
        }

        protected override IEnumerable<string> GetNonMatchingUpdateExtensions()
        {
            yield return GuruTestData.UpdateApp;
            yield return GuruTestData.UpdateSip;
            yield return GuruTestData.UpdatePremium;
            yield return GuruTestData.UpdateGsm;
        }

        protected override IEnumerable<string> GetNonMatchingCreateExtensions()
        {
            return GetNonMatchingUpdateExtensions();
        }

        protected override GuruData GetMatchingUpdateExtension()
        {
            var result = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect);
            result.Password = "asdf";
            return result;
        }

        protected override GuruData GetMatchingCreateExtension()
        {
            return null;
        }

        protected override GuruData GetMatchingCreateTrunk()
        {
            return JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"announcement_lang\": \"de-DE\",  \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"DECT\", \"password\": \"PRQ0Y8lX\", \"isPremium\": false}");
        }

        protected override void ValidateUser(DectUser user)
        {
            Assert.Equal("user", user.Type);
            Assert.Equal("asdf", user.Password);
            Assert.Equal(DectDisplayModus.NumName, user.DisplayMode);
        }
    }
}