using System;
using epmgr.Data;
using epmgr.Guru;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace epmgr.test
{
    public class GuruClientTest
    {
        [Fact(Skip = "Integration")]
        public async Task CanGetMessages()
        {
            using (var client = new GuruClient("https://guru3.eventphone.de", "<private>", "3"))
            {
                var messages = await client.GetMessagesAsync(CancellationToken.None);
                Assert.NotEmpty(messages);
            }
        }

        [Fact]
        public void CanReadMessage()
        {
            var message = "{\"type\": \"UPDATE_EXTENSION\", \"id\": 68, \"timestamp\": 1520693798.597209, " +
                "\"data\": {\"number\": \"4502\", \"type\": \"DECT\", \"displayModus\": \"NUMBER_NAME\", " +
                "\"token\": \"67789479\", \"location\": \"\", \"useEncryption\": false, \"announcement_lang\": \"en-GB\"}}";
            var guruMessage = JsonConvert.DeserializeObject<GuruMessage>(message);
            Assert.Equal(MgrExtensionType.DECT, guruMessage.Data.Type);
            Assert.Equal("en-GB", guruMessage.Data.Language);
            message = "{\"type\": \"UPDATE_EXTENSION\", \"id\": 68, \"timestamp\": 1520693798.597209, " +
                "\"data\": {\"number\": \"4502\", \"type\": \"SIP\", " +
                "\"password\": \"67789479\", \"location\": \"\", \"isPremium\": true, \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"8a94e92ef5073d861116b3573f49bc9ab63c53c76def73644ad1864d4ce6f917f52bb8f01aa02362199966ed67faf76a1ebd62fa28664184fa02ee0cd4e782fa\"}}";
            guruMessage = JsonConvert.DeserializeObject<GuruMessage>(message);
            Assert.Equal(MgrExtensionType.PREMIUM, guruMessage.Data.Type);
            Assert.Equal("de-DE", guruMessage.Data.Language);
            Assert.Equal("8a94e92ef5073d861116b3573f49bc9ab63c53c76def73644ad1864d4ce6f917f52bb8f01aa02362199966ed67faf76a1ebd62fa28664184fa02ee0cd4e782fa", guruMessage.Data.RingbackTone);
        }

        [Fact]
        public void CanReadGuruMessageWithHandset()
        {
            var message = "{\"id\":38,\"timestamp\":1553948202.673443,\"type\":\"UPDATE_EXTENSION\",\"data\":{" +
                          "\"number\":\"7217\",\"name\":\"\",\"location\":\"\",\"announcement_lang\":\"de-DE\"," +
                          "\"ringback_tone\":\"\",\"type\":\"DECT\",\"token\":\"17414922\",\"useEncryption\":false," +
                          "\"displayModus\":\"NUMBER_NAME\",\"dect\":{\"ipei\":\"00000 6789055 3\",\"uak\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"}}}";
            var guruMessage = JsonConvert.DeserializeObject<GuruMessage>(message);
            Assert.Equal(MgrExtensionType.DECT, guruMessage.Data.Type);
            Assert.NotNull(guruMessage.Data.Dect);
            Assert.Equal("00000 6789055 3", guruMessage.Data.Dect.Ipei);
            Assert.Equal("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", guruMessage.Data.Dect.Uak);
        }

        [Fact]
        public void CanSerializeDeviceMessage()
        {
            var msg = new GuruMessage
            {
                Id = 10,
                Type = GuruMessageType.AssignHandset,
                Timestamp = 1550505247,
                Data = new GuruData
                {
                    Ipei = "10345 0615703 *",
                    Uak = "5C4D929813BD0A74A281F79CAFAE858F",
                    Extension = "4502"
                }
            };
            Assert.Equal("{" +
                            "\"type\":\"ASSIGN_HANDSET\"," +
                            "\"id\":10," +
                            "\"timestamp\":1550505247.0," +
                            "\"data\":{" +
                            "\"ipei\":\"10345 0615703 *\"," +
                            "\"uak\":\"5C4D929813BD0A74A281F79CAFAE858F\"," +
                            "\"extension\":\"4502\"" +
                            "}" +
                            "}",
                JsonConvert.SerializeObject(msg, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore}));
        }
    }
}
