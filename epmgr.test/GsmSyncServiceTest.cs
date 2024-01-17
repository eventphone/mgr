using System.Threading;
using System.Threading.Tasks;
using epmgr.Gsm;
using epmgr.Guru;
using epmgr.Services;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class GsmSyncServiceTest
    {
        private readonly Mock<IGsmClient> _gsmClientMock = new Mock<IGsmClient>();
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
        public async Task UpdateExtensionWithNonMatchingTypeTriggersDelete()
        {
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateApp);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateDect);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdatePremium);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateSip);
            await NonMatchingTypeUpdateExtension(GuruTestData.UpdateSpecial);
        }
        private async Task NonMatchingTypeUpdateExtension(string update)
        {
            var service = GetService();
            _gsmClientMock.Invocations.Clear();

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(update)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("4711", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateExtensionForNonExistingExtensionIsIgnored()
        {
            var service = GetService();

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateGsm)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("4711", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateExtensionUpdatesUmts()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("4711", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriberInfo{Number = "4712"});
            _gsmClientMock.Setup(x => x.SetUmtsEnabledAsync("4711", true, It.IsAny<CancellationToken>()));
            
            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateGsm)};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("4711", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.Verify(x=>x.SetUmtsEnabledAsync("4712", true, It.IsAny<CancellationToken>()), Times.Once());
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteExtensionForNonExistingGsmIsIgnored()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()));

            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteExtensionResetsGsm()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriberInfo{Number = "2002"});

            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.Verify(x=>x.ResetExtensionAsync("2002", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RenameExtension()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("4503", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriberInfo{Number = "4503"});
            
            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("4503", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.Verify(x=>x.UpdateExtensionAsync("4503", null, "4502", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RenameNonExistingExtensionIsIgnored()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("4503", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<SubscriberInfo>(null));
            
            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("4503", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnsubscribeDeviceForNonExistingGsmIsIgnored()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()));

            var guruData = new GuruMessage {Type = "UNSUBSCRIBE_DEVICE", Data = JsonConvert.DeserializeObject<GuruData>("{\"extension\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnsubscribeDevice()
        {
            var service = GetService();
            _gsmClientMock.Setup(x => x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriberInfo{Number = "2002"});
            
            var guruData = new GuruMessage {Type = "UNSUBSCRIBE_DEVICE", Data = JsonConvert.DeserializeObject<GuruData>("{\"extension\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
            
            _gsmClientMock.Verify(x=>x.GetSubscriberAsync("2001", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.Verify(x=>x.ResetExtensionAsync("2002", It.IsAny<CancellationToken>()), Times.Once);
            _gsmClientMock.VerifyNoOtherCalls();

        }

        private GsmSyncService GetNullService()
        {
            return new GsmSyncService(null);
        }

        private GsmSyncService GetService()
        {
            return new GsmSyncService(_gsmClientMock.Object);
        }
    }
}