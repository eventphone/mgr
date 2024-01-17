using System.Collections.Generic;
using System.Linq;
using epmgr.Guru;
using epmgr.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace epmgr.test
{
    public class GuruSyncServiceTest
    {
        [Fact]
        public void CanCreateBatches()
        {
            var service = GetService();
            var messages = new[]
            {
                new GuruMessage {Type = GuruMessageType.UpdateExtension, Data = new GuruData {Number = "4502"}},
                new GuruMessage {Type = GuruMessageType.UpdateExtension, Data = new GuruData {Number = "2001"}},
                new GuruMessage {Type = GuruMessageType.UpdateGroup, Data = new GuruData {Number = "2000", Extensions = new[] {new GuruExtension{Extension = "4502", Active = true}, new GuruExtension{Extension = "2001", Active = true}}}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "2005"}},
                new GuruMessage {Type = GuruMessageType.UpdateGroup, Data = new GuruData {Number = "1000", Extensions = new[] {new GuruExtension{Extension = "4502", Active = true}}}},
                new GuruMessage {Type = GuruMessageType.UpdateExtension, Data = new GuruData {Number = "2004"}},
                new GuruMessage {Type = GuruMessageType.UnsubscribeDevice, Data = new GuruData {Extension = "2004"}},
                new GuruMessage {Type = GuruMessageType.RenameExtension, Data = new GuruData {OldExtension = "2004", NewExtension = "2003"}},
                new GuruMessage {Type = GuruMessageType.UpdateGroup, Data = new GuruData {Number = "1001", Extensions = new[] {new GuruExtension{Extension = "1000", Active = true}, new GuruExtension{Extension = "2000", Active = true}}}},
            };
            var batches = service.CreateBatches(messages).ToList();
            Assert.Single(batches);
            Assert.Equal(messages.Length, batches.SelectMany(x=>x).Count());
        }

        [Fact]
        public void ResyncIsSingleBatch()
        {
            var service = GetService();
            var messages = new List<GuruMessage>
            {
                new GuruMessage {Type = GuruMessageType.UpdateExtension, Data = new GuruData {Number = "1000"}},
                new GuruMessage {Type = GuruMessageType.UpdateExtension, Data = new GuruData {Number = "1001"}},

                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1002"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1003"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1004"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1005"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1006"}},

                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1007"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1008"}},
                new GuruMessage {Type = GuruMessageType.DeleteExtension, Data = new GuruData {Number = "1009"}},
            };
            var batches = service.CreateBatches(messages).ToList();
            Assert.Single(batches);
            Assert.Equal(messages.Count, batches.SelectMany(x=>x).Count());
            
            messages.Insert(2, new GuruMessage{Type = GuruMessageType.StartResync});
            batches = service.CreateBatches(messages).ToList();
            Assert.Equal(3, batches.Count);
            Assert.Single(batches[1]);
            Assert.Equal(messages.Count, batches.SelectMany(x=>x).Count());

            messages.Insert(9, new GuruMessage{Type = GuruMessageType.EndResync});
            batches = service.CreateBatches(messages).ToList();
            Assert.Equal(5, batches.Count);
            Assert.Single(batches[1]);
            Assert.Single(batches[3]);
            Assert.Equal(messages.Count, batches.SelectMany(x=>x).Count());
        }

        private GuruSyncService GetService()
        {
            return new GuruSyncService(new NullLoggerFactory(), new OptionsWrapper<GuruSettings>(null), null, null, null);
        }
    }
}