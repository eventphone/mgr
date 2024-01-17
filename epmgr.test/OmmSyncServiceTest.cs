using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Omm;
using epmgr.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using mitelapi;
using mitelapi.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class OmmSyncServiceTest
    {
        private readonly Mock<IOmmClient> _ommClientMock = new Mock<IOmmClient>();
        private readonly Mock<IServiceProvider> _serviceProviderMock = new Mock<IServiceProvider>();
        private readonly Mock<IExtensionService> _extensionServiceMock = new Mock<IExtensionService>();
        private readonly Mock<IOptions<Settings>> _optionsMock = new Mock<IOptions<Settings>>();

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
        public async Task UpdateExtensionWithNonMatchingTypeIsIgnored()
        {
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateApp);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateGsm);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdatePremium);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateSip);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateSpecial);
        }
        private async Task NonMatchingTypeUpdateExtension(string update)
        {
            var service = GetNullService();
            
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(update)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task UpdateExtensionForNonExistingExtensionIsIgnored()
        {
            var service = GetService();
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>())).ThrowsAsync(new OmmNoEntryException(""));
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>()), Times.Once());
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateExtensionUpdatesName()
        {
            var service = GetService();
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPUserType{Uid = 4712});
            
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _ommClientMock.Verify(x=>x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>()), Times.Once());
            _ommClientMock.Verify(x=>x.SetPPUserAsync(It.Is<PPUserType>(p=>p.Name == "Unit Test" && p.Uid == 4712), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateExtensionUpdatesEncryption()
        {
            var service = GetService();

            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPUserType { Ppn = 4712 });

            var guruData = new GuruMessage { Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect) };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("4711", It.IsAny<CancellationToken>()), Times.Once());
            _ommClientMock.Verify(x => x.SetPPDevAsync(It.Is<PPDevType>(p => p.Encrypt == false && p.Ppn == 4712), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public Task DeleteExtensionRemovesYwsdAndAttachesTempExtension()
        {
            var service = GetService();
            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IServiceScopeFactory))))
                .Returns(new ServiceScopeFactory(_serviceProviderMock.Object));
            _ommClientMock.Setup(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>())).ReturnsAsync(new PPUserType { Uid = 4712, Name = "DECT Temp" });
            return DeleteExistingExtension(service);
        }

        private async Task DeleteExistingExtension(OmmSyncService service)
        {
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPUserType { Ppn = 4711, Uid = 1337 });

            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync(It.Is<string>(x => x.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            var guruData = new GuruMessage { Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}") };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.DeletePPUserAsync(1337, It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.SetPPAsync(It.Is<PPDevType>(p => p.Ppn == 4711), It.Is<PPUserType>(p => p.Uid == 4712), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteExtensionForNonExistingDectIsIgnored()
        {
            var service = GetService();
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            var guruData = new GuruMessage { Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}") };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()));
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateTempExtensionTriggersYwsdExtensionCreation()
        {
            var service = GetService();

            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IEnumerable<IExtensionService>))))
                .Returns(new[] { _extensionServiceMock.Object });
            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IServiceScopeFactory))))
                .Returns(new ServiceScopeFactory(_serviceProviderMock.Object));
            _optionsMock.Setup(x => x.Value).Returns(new Settings { DefaultLanguage = "Unit Test" });
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync(It.Is<string>(s => s.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));
            _ommClientMock.Setup(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>())).ReturnsAsync(new PPUserType { Uid = 4712, Name = "DECT Temp" });

            await DeleteExistingExtension(service);

            _extensionServiceMock.Verify(x => x.CreateExtension(It.Is<MgrCreateExtension>(e =>
                  e.Type == MgrExtensionType.DECT && e.Number.StartsWith(Settings.DectTempPrefix) && e.Name == "DECT Temp"), It.IsAny<CancellationToken>()), Times.Once);
            _extensionServiceMock.VerifyNoOtherCalls();
            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync(It.Is<string>(s => s.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateTempExtensionDoesNotTriggerSelf()
        {
            var service = GetService();

            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IEnumerable<IExtensionService>))))
                .Returns(new[] { service });
            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IServiceScopeFactory))))
                .Returns(new ServiceScopeFactory(_serviceProviderMock.Object));
            _optionsMock.Setup(x => x.Value).Returns(new Settings { DefaultLanguage = "Unit Test" });
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync(It.Is<string>(s => s.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));
            _ommClientMock.Setup(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>())).ReturnsAsync(new PPUserType { Uid = 4712, Name = "DECT Temp" });

            await DeleteExistingExtension(service);

            _extensionServiceMock.VerifyNoOtherCalls();
            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync(It.Is<string>(s => s.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public Task CreateExtensionWithDeviceCreatesExtension()
        {
            var service = GetService();
            return CreateExtensionWithDevice(service);
        }

        [Fact]
        public async Task CreateExtensionWithDeviceDoesNotTriggerExtensionservice()
        {
            var service = GetService();

            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IEnumerable<IExtensionService>))))
                .Returns(new[] { _extensionServiceMock.Object });
            _optionsMock.Setup(x => x.Value).Returns(new Settings { DefaultLanguage = "Unit Test" });

            await CreateExtensionWithDevice(service);

            _extensionServiceMock.VerifyNoOtherCalls();
        }

        private async Task CreateExtensionWithDevice(OmmSyncService service)
        {
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4502", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            _ommClientMock.Setup(x => x.GetPPDevByIPEIAsync("10345 0615703 *", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            _ommClientMock.Setup(x => x.CreatePPDevAsync(It.Is<PPDevType>(x => x.Uak == "AAAABBBBCCCCDDDDEEEEFFFF00001111"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPDevType());
            _ommClientMock.Setup(x => x.CreatePPUserAsync(It.IsAny<PPUserType>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PPUserType());

            var guruData = new GuruMessage
            {
                Type = "UPDATE_EXTENSION",
                Data = JsonConvert.DeserializeObject<GuruData>(
                "{\"number\": \"4502\", \"name\": \"PoC zivillian\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", " +
                "\"ringback_tone\": \"\", \"type\": \"DECT\", \"token\": \"96625287\", \"useEncryption\": true, " +
                "\"displayModus\": \"NUMBER_NAME\", \"dect\": {\"ipei\": \"10345 0615703 *\", " +
                "\"uak\": \"AAAABBBBCCCCDDDDEEEEFFFF00001111\"}}")
            };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x=>x.GetPPUserByNumberAsync("4502", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num == "4502"), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.CreatePPDevAsync(It.Is<PPDevType>(p => p.Ipei == "10345 0615703 *"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RenameNonExistingExtensionIsIgnored()
        {
            var service = GetService();

            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4503", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            var guruData = new GuruMessage { Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}") };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("4503", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RenameExtension()
        {
            var service = GetService();

            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("4503", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPUserType { Uid = 4711});

            var guruData = new GuruMessage { Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}") };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("4503", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.SetPPUserAsync(It.Is<PPUserType>(u => u.AddId == "4502" && u.SipAuthId == "4502" && u.Num == "4502"), It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnsubscribeDeviceForNonExistingDectIsIgnored()
        {
            var service = GetService();
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));

            var guruData = new GuruMessage { Type = "UNSUBSCRIBE_DEVICE", Data = new GuruData { Extension = "2001" } };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnsubscribeDevice()
        {
            var service = GetService();
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PPUserType { Uid = 1337, Ppn = 4711 });
            _ommClientMock.Setup(x => x.GetPPUserByNumberAsync(It.Is<string>(x=>x.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OmmNoEntryException(""));
            _serviceProviderMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IServiceScopeFactory))))
                .Returns(new ServiceScopeFactory(_serviceProviderMock.Object));
            _ommClientMock.Setup(x => x.CreatePPUserAsync(It.Is<PPUserType>(p => p.Num.StartsWith(Settings.DectTempPrefix)), It.IsAny<CancellationToken>())).ReturnsAsync(new PPUserType { Uid = 4712, Name = "DECT Temp" });

            var guruData = new GuruMessage { Type = "UNSUBSCRIBE_DEVICE", Data = new GuruData { Extension = "2001" } };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _ommClientMock.Verify(x => x.GetPPUserByNumberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.DeletePPUserAsync(1337, It.IsAny<CancellationToken>()), Times.Once);
            _ommClientMock.Verify(x => x.SetPPAsync(It.Is<PPDevType>(p => p.Ppn == 4711), It.Is<PPUserType>(p => p.Uid == 4712), It.IsAny<CancellationToken>()), Times.Once);
        }

        private OmmSyncService GetService()
        {
            return new OmmSyncService(NullLogger<OmmSyncService>.Instance, _optionsMock.Object, _ommClientMock.Object, new RandomPasswordService(), _serviceProviderMock.Object);
        }

        private OmmSyncService GetNullService()
        {
            return new OmmSyncService(null, null, null, null, null);
        }

        class ServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IServiceProvider _serviceProvider;

            public ServiceScopeFactory(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public IServiceScope CreateScope()
            {
                return new ServiceScope(_serviceProvider);
            }

            class ServiceScope : IServiceScope
            {
                public ServiceScope(IServiceProvider serviceProvider)
                {
                    ServiceProvider = serviceProvider;
                }

                public void Dispose()
                {}

                public IServiceProvider ServiceProvider { get; }
            }
        }
    }
}