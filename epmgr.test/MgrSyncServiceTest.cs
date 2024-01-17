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
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class MgrSyncServiceTest
    {
        private DbContextOptions<MgrDbContext> _mgrDbOptions;

        private readonly Mock<IServiceProvider> _serviceProviderMock = new Mock<IServiceProvider>();
        private readonly Mock<IExtensionService> _extensionServiceMock = new Mock<IExtensionService>();
        private readonly RandomPasswordService _passwordService = new RandomPasswordTestService();

        private readonly GuruMessage _updateAppMessage = new GuruMessage
        {
            Type = "UPDATE_EXTENSION",
            Data = JsonConvert.DeserializeObject<GuruData>(
                "{\"number\":\"9489\",\"name\":\"WITZ\",\"location\":\"\",\"announcement_lang\":\"de-DE\",\"ringback_tone\":\"\",\"type\":\"APP\"}")
        };

        [Fact]
        public async Task StartSyncSetsDeleteAfterResync()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 1, Extension = "2000", DeleteAfterResync = false});
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "2001", DeleteAfterResync = false});
                context.Extensions.Add(new MgrExtension {Id = 3, Extension = "2005", DeleteAfterResync = false});
                context.SaveChanges();
            }

            var guruData = new GuruMessage {Type = "SYNC_STARTED", Data = new GuruData(), Id = 47193};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                Assert.True(context.Extensions.Any());
                Assert.True(context.Extensions.All(x=>x.DeleteAfterResync));
            }
        }

        [Fact]
        public async Task EndSyncDeletesExtensions()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 1, Extension = "2000", DeleteAfterResync = false});
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "2001", DeleteAfterResync = true});
                context.Extensions.Add(new MgrExtension {Id = 3, Extension = "2005", DeleteAfterResync = false});
                context.SaveChanges();
            }
            var guruData = new GuruMessage {Type = "SYNC_ENDED", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                Assert.Equal(2, context.Extensions.Count());
                Assert.True(context.Extensions.All(x=>!x.DeleteAfterResync));
            }
        }

        [Fact]
        public async Task EndSyncTriggerDeleteOnIExtensionServices()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "2001", DeleteAfterResync = true});
                context.SaveChanges();
            }
            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IEnumerable<IExtensionService>))))
                .Returns(new[] {_extensionServiceMock.Object});
            
            var guruData = new GuruMessage {Type = "SYNC_ENDED", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _extensionServiceMock.Verify(x=>x.DeleteExtension("2001", It.IsAny<CancellationToken>()),Times.Once);
        }

        [Fact]
        public async Task UpdateExtensionAddsMgrExtension()
        {
            var service = GetService();

            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("9489", extension.Extension);
                Assert.Equal("WITZ", extension.Name);
                Assert.Equal("de-DE", extension.Language);
                Assert.False(extension.DeleteAfterResync);
            }
        }
        
        [Fact]
        public async Task UpdateExtensionUpdatesMgrExtension()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "9489", DeleteAfterResync = true, Password = "asdf", UseEncryption = true});
                context.SaveChanges();
            }

            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("9489", extension.Extension);
                Assert.False(extension.DeleteAfterResync);
                Assert.False(extension.UseEncryption);
                Assert.True(String.IsNullOrEmpty(extension.Password));
            }
        }

        [Fact]
        public async Task CreateDectExtensionGeneratesSipPassword()
        {
            var service = GetService();

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)};
            Assert.NotEqual("<random>", guruData.Data.Password);
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            Assert.Equal("<random>", guruData.Data.Password);
        }

        [Fact]
        public async Task ChangeTypeToDectGeneratesSipPassword()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "4711", DeleteAfterResync = true, UseEncryption = true, Type = MgrExtensionType.ANNOUNCEMENT});
                context.SaveChanges();
            }
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)};
            Assert.True(String.IsNullOrEmpty(guruData.Data.Password));
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            Assert.False(String.IsNullOrEmpty(guruData.Data.Password));
        }

        [Fact]
        public async Task UpdateDectExtensionReusesSipPassword()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "4711", DeleteAfterResync = true, Password = "asdf", UseEncryption = true});
                context.SaveChanges();
            }
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)};
            Assert.NotEqual("asdf", guruData.Data.Password);
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            Assert.Equal("asdf", guruData.Data.Password);

        }

        [Fact]
        public async Task CreateGsmExtensionGeneratesSipPassword()
        {
            var service = GetService();

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateGsm)};
            Assert.NotEqual("<random>", guruData.Data.Password);
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            Assert.Equal("<random>", guruData.Data.Password);
        }

        [Fact]
        public async Task UpdateGsmExtensionReusesSipPassword()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "4711", DeleteAfterResync = true, Password = "asdf", UseEncryption = true});
                context.SaveChanges();
            }
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateGsm)};
            Assert.NotEqual("asdf", guruData.Data.Password);
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            Assert.Equal("asdf", guruData.Data.Password);

        }

        [Fact]
        public async Task DeleteExtensionRemovesMgrExtension()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "9489", DeleteAfterResync = true, Password = "asdf", UseEncryption = true});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"9489\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                Assert.Empty(context.Extensions);
            }
        }

        [Fact]
        public async Task DeleteExtensionForNonExistingIsIgnored()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 2, Extension = "9489", DeleteAfterResync = true, Password = "asdf", UseEncryption = true});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                Assert.NotEmpty(context.Extensions);
            }
        }

        [Fact]
        public async Task CanDeleteNonExistingExtension()
        {
            var service = GetService();

            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"9489\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                Assert.Empty(context.Extensions);
            }
        }

        [Fact]
        public async Task UpdateCallGroupIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP"};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        private static MgrSyncService GetNullService()
        {
            return new MgrSyncService(null, null, null, null);
        }

        [Fact]
        public async Task RenameExtension()
        {
            var service = GetService();
            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                context.Extensions.Add(new MgrExtension {Id = 10, Extension = "4503"});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new MgrDbContext(_mgrDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal("4502", extension.Extension);
                Assert.Equal(10, extension.Id);
            }
        }

        [Fact]
        public async Task UnsubscribeDeviceIsIgnored()
        {
            var service = GetNullService();

            var guruData = new GuruMessage {Type = "UNSUBSCRIBE_DEVICE", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        private MgrSyncService GetService([CallerMemberName] string caller = "")
        {
            _mgrDbOptions = new DbContextOptionsBuilder<MgrDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new MgrSyncService(NullLogger<MgrSyncService>.Instance, new MgrDbContext(_mgrDbOptions), _serviceProviderMock.Object, _passwordService);
        }

        class RandomPasswordTestService : RandomPasswordService
        {
            public override string GenerateSipPassword()
            {
                return "<random>";
            }
        }
    }
}