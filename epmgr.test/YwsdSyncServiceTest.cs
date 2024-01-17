using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using epmgr.Guru;
using epmgr.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace epmgr.test
{
    public class YwsdSyncServiceTest
    {
        private DbContextOptions<YwsdDbContext> _ywsdDbOptions;
        
        private readonly GuruMessage _updateAppMessage = new GuruMessage
        {
            Type = "UPDATE_EXTENSION",
            Data = JsonConvert.DeserializeObject<GuruData>(
                "{\"number\":\"9489\",\"name\":\"WITZ\",\"location\":\"\",\"announcement_lang\":\"de-DE\",\"ringback_tone\":\"\",\"type\":\"APP\"}")
        };

        private readonly GuruMessage _updateSpecialMessage = new GuruMessage
        {
            Type = "UPDATE_EXTENSION",
            Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4711\", \"name\": \"Unit Test\", \"location\": \"empty\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"SPECIAL\"}")
        };

        private readonly GuruMessage _updateGroupMessage = new GuruMessage
        {
            Type = "UPDATE_EXTENSION",
            Data = JsonConvert.DeserializeObject<GuruData>(
                "{\"number\": \"2000\", \"name\": \"PoC Service Hotline\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"GROUP\"}")
        };

        [Fact]
        public async Task StartSyncIsIgnored()
        {
            var service = new YwsdSyncService(null, null);
            
            var guruData = new GuruMessage {Type = "SYNC_STARTED", Data = new GuruData(), Id = 47193};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task EndSyncIsIgnored()
        {
            var service = new YwsdSyncService(null, null);

            var guruData = new GuruMessage {Type = "SYNC_ENDED", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task UnsubscribeDeviceIsIgnored()
        {
            var service = new YwsdSyncService(null, null);

            var guruData = new GuruMessage {Type = "UNSUBSCRIBE_DEVICE", Data = new GuruData()};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);
        }

        [Fact]
        public async Task DeleteExtensionCanBeTriggered()
        {
            var service = GetService();

            IExtensionService extensionService = service;
            await extensionService.DeleteExtension("1234", CancellationToken.None);
        }

        [Fact]
        public async Task DeleteExtensionTriggerRemovesExtension()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension
                {
                    Id = 1, Number = "1234", Name = "Test", Type = ExtensionType.Simple, Language = "de-DE",
                    Ringback = ""
                });
                context.SaveChanges();
            }

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Single(context.Extensions);
            }

            await service.DeleteExtension("1234", CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.Extensions);
            }
        }

        [Fact]
        public async Task UpdateExtensionAddsYwsdExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.SaveChanges();
            }

            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("9489", extension.Number);
                Assert.Equal("WITZ", extension.Name);
                Assert.Equal("", extension.Ringback);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal(ForwardingMode.Disabled, extension.ForwardingMode);
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Equal(2, extension.YateId);

                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateNonExistingDectExtensionCreatesExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.SaveChanges();
            }

            var guruData = new GuruMessage
            {
                Type = "UPDATE_EXTENSION",
                Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)
            };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("4711", extension.Number);
                Assert.Equal("Unit Test", extension.Name);
                Assert.Equal("", extension.Ringback);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal(ForwardingMode.Disabled, extension.ForwardingMode);
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Equal(1, extension.YateId);
            }
        }

        [Fact]
        public async Task UpdateExistingDectExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension
                {
                    Id = 1, 
                    Number = "4711", 
                    Name = "WATZ", 
                    Ringback = "empty", 
                    Language = "en-US",
                    OutgoingNumber = "0000", 
                    OutgoingName = "Test", 
                    ForwardingDelay = 5,
                    ForwardingMode = ForwardingMode.Enabled,
                    ForwardingExtensionId = 13,
                    YateId = 1,
                    Type = ExtensionType.Simple
                });
                context.SaveChanges();
            }

            var guruData = new GuruMessage
            {
                Type = "UPDATE_EXTENSION",
                Data = JsonConvert.DeserializeObject<GuruData>(GuruTestData.UpdateDect)
            };
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("Unit Test", extension.Name);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal("", extension.Ringback);
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Equal(1, extension.YateId);
            }
        }

        [Fact]
        public async Task UpdateSpecialExtensionIsIgnored()
        {
            var service = GetService();

            var guruData = _updateSpecialMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.Extensions);
            }
        }
        
        [Fact]
        public async Task UpdateGroupExtensionAddsYwsdExtension()
        {
            
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.SaveChanges();
            }

            var guruData = _updateGroupMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("2000", extension.Number);
                Assert.Equal("PoC Service Hotline", extension.Name);
                Assert.Equal("", extension.Ringback);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal(ForwardingMode.Disabled, extension.ForwardingMode);
                Assert.Equal(ExtensionType.Group, extension.Type);
                Assert.Null(extension.YateId);
            }
        }

        [Fact]
        public async Task UpdateExtensionUpdatesYwsdExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension
                {
                    Id = 1, 
                    Number = "9489", 
                    Name = "WATZ", 
                    Ringback = "empty", 
                    Language = "en-US",
                    OutgoingNumber = "0000", 
                    OutgoingName = "Test", 
                    ForwardingDelay = 5,
                    ForwardingMode = ForwardingMode.Enabled,
                    ForwardingExtensionId = 13,
                    YateId = 2,
                    Type = ExtensionType.Simple
                });
                context.SaveChanges();
            }
            
            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("9489", extension.Number);
                Assert.Equal("WITZ", extension.Name);
                Assert.Equal("", extension.Ringback);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Equal(2, extension.YateId);

                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateExtensionDoesNotUpdateUnspportedPropertiesYwsdExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension
                {
                    Id = 1, 
                    Number = "9489", 
                    Name = "WATZ", 
                    Ringback = "empty", 
                    Language = "en-US",
                    OutgoingNumber = "0000", 
                    OutgoingName = "Test", 
                    YateId = 2,
                    Type = ExtensionType.Simple
                });
                context.SaveChanges();
            }
            
            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.Equal("Test", extension.OutgoingName);
                Assert.Equal("0000", extension.OutgoingNumber);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithDifferentTypeRecreatesExtension()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension
                {
                    Id = 1337, 
                    Number = "9489",
                    Type = ExtensionType.Group
                });
                context.SaveChanges();
            }
            
            var guruData = _updateAppMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.NotEqual(1337, extension.Id);
                Assert.Equal("WITZ", extension.Name);
                Assert.Equal("", extension.Ringback);
                Assert.Equal("de-DE", extension.Language);
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Equal(2, extension.YateId);

                Assert.Empty(context.ForkRanks);
            }
        }
        
        [Fact]
        public async Task UpdateGroupExtensionDoesNotAddYwsdForkRank()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.SaveChanges();
            }

            var guruData = _updateGroupMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateGroupExtensionDoesNotRecreateYwsdForkRank()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension {Number = "2000", Type = ExtensionType.Group});
                context.SaveChanges();
            }

            var guruData = _updateGroupMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateGroupExtensionDoesNotUpdateYwsdForkRank()
        {
            var service = GetService();

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-app", Guru3Identifier = "APP"});
                context.Extensions.Add(new Extension {Id=1, Number = "2000", Type = ExtensionType.Group});
                context.ForkRanks.Add(new ForkRank {ExtensionId = 1, Index = 2, Delay = 500, Mode = ForkRankMode.Default});
                context.ForkRanks.Add(new ForkRank {ExtensionId = 1, Index = 3, Mode = ForkRankMode.Next});
                context.SaveChanges();
            }

            var guruData = _updateGroupMessage;
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Equal(2, context.ForkRanks.Count());
                var defaultRank = context.ForkRanks.Single(x => x.Mode == ForkRankMode.Default);
                Assert.Equal(500, defaultRank.Delay);
                Assert.Equal(2, defaultRank.Index);
            }
        }

        [Fact]
        public async Task UpdateGroupCreatesYwsdForkRankMember()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [{\"extension\": \"2001\", \"active\": true, \"delay\":0}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var member = context.ForkRankMember.Single();
                Assert.Equal(20, member.ExtensionId);
                Assert.Equal(10, member.ForkRankId);
                Assert.True(member.IsActive);
                Assert.Equal(ForkRankMemberType.Default, member.Type);
            }
        }

        [Fact]
        public async Task UpdateGroupRemovesYwsdForkRankMember()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [{\"extension\": \"2001\", \"active\": true, \"delay\":0}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Single(context.ForkRanks);
                var member = Assert.Single(context.ForkRanks.SelectMany(x=>x.ForkRankMember));
                Assert.Equal(20, member.ExtensionId);
                Assert.Equal(10, member.ForkRankId);
                Assert.True(member.IsActive);
                Assert.Equal(ForkRankMemberType.Default, member.Type);
            }
        }

        [Fact]
        public async Task UpdateGroupReplacesYwsdForkRankMember()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [{\"extension\": \"2001\", \"active\": true, \"delay\":0}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var member = context.ForkRankMember.Single();
                Assert.Equal(20, member.ExtensionId);
                Assert.Equal(10, member.ForkRankId);
                Assert.True(member.IsActive);
                Assert.Equal(ForkRankMemberType.Default, member.Type);
            }
        }

        [Fact]
        public async Task UpdateGroupRemovesAdditionalYwsdForkRanks()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 1, Mode = ForkRankMode.Default});
                context.ForkRanks.Add(new ForkRank {Id = 20, ExtensionId = 10, Index = 1, Mode = ForkRankMode.Next});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 20, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": []}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task DeleteExtensionRemovesYwsdExtension()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Number = "2001", Type = ExtensionType.Simple});
                context.SaveChanges();
            }
            
            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"2001\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single();
                Assert.NotEqual("2001", extension.Number);
            }
        }

        [Fact]
        public async Task DeleteExtensionForNonExistingIsIgnored()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Number = "2001", Type = ExtensionType.Simple});
                context.SaveChanges();
            }
            
            var guruData = new GuruMessage{Type = "DELETE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\":\"1234\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.NotEmpty(context.Extensions);
            }
        }

        [Fact]
        public async Task UpdateExtensionForUnknowYateThrowsException()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-dect", Guru3Identifier = "DECT"});
                context.Yates.Add(new Yate {Id = 2, Hostname = "yate-sip", Guru3Identifier = "SIP"});
                context.SaveChanges();
            }

            var guruData = _updateAppMessage;
            await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ProcessMessageAsync(guruData, CancellationToken.None));

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Empty(context.Extensions);
            }
        }

        [Fact]
        public async Task UpdateGroupMemberUpdatesActiveFlag()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": false, \"delay\":0}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Equal(2, context.ForkRankMember.Count());
                var dect = context.ForkRankMember.Single(x => x.Extension.Number == "2001");
                var sip = context.ForkRankMember.Single(x => x.Extension.Number == "2005");
                Assert.True(dect.IsActive);
                Assert.False(sip.IsActive);
            }
        }

        [Fact]
        public async Task UpdateGroupMemberCalculatesForkRanks()
        {
            
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": true, \"delay\":10}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Equal(2, context.ForkRanks.Count());
                var first = Assert.Single(context.ForkRanks.Where(x => x.Delay == 0));
                var sip = Assert.Single(context.ForkRanks.Where(x => x.Id == first.Id).SelectMany(x => x.ForkRankMember).Select(x=>x.Extension));
                Assert.Equal("2001", sip.Number);
                var second =Assert.Single(context.ForkRanks.Where(x => x.Delay != 0));
                var dect = Assert.Single(context.ForkRanks.Where(x => x.Id == second.Id).SelectMany(x => x.ForkRankMember).Select(x=>x.Extension));
                Assert.Equal(10000, second.Delay);
                Assert.Equal("2005", dect.Number);
            }
        }

        [Fact]
        public async Task UpdateGroupMemberUpdatesDelay()
        {
            
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default, Delay = 0});
                context.ForkRanks.Add(new ForkRank {Id = 20, ExtensionId = 10, Index = 1, Mode = ForkRankMode.Default, Delay = 20000});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 20, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": true, \"delay\":10}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Equal(2, context.ForkRanks.Count());
                var first = Assert.Single(context.ForkRanks.Where(x => x.Delay == 0));
                var sip = Assert.Single(context.ForkRanks.Where(x => x.Id == first.Id).SelectMany(x => x.ForkRankMember).Select(x=>x.Extension));
                Assert.Equal("2001", sip.Number);
                var second =Assert.Single(context.ForkRanks.Where(x => x.Delay != 0));
                var dect = Assert.Single(context.ForkRanks.Where(x => x.Id == second.Id).SelectMany(x => x.ForkRankMember).Select(x=>x.Extension));
                Assert.Equal(10000, second.Delay);
                Assert.Equal("2005", dect.Number);
            }
        }

        [Fact]
        public async Task UpdateGroupMemberRemovesEmptyForkRanks()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default, Delay = 0});
                context.ForkRanks.Add(new ForkRank {Id = 20, ExtensionId = 10, Index = 1, Mode = ForkRankMode.Default, Delay = 20000});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 20, ForkRankId = 10, IsActive = true, Type = ForkRankMemberType.Default});
                context.ForkRankMember.Add(new ForkRankMember {ExtensionId = 30, ForkRankId = 20, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": true, \"delay\":0}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                Assert.Single(context.ForkRanks);
            }
        }

        [Fact]
        public async Task ForkRankDelayIsCalculatedAsDelta()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 40, Number = "2002", Type = ExtensionType.Simple});
                context.SaveChanges();
            }
            
            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": true, \"delay\":10}," +
                                                                                                                     "{\"extension\": \"2002\", \"active\": true, \"delay\":30}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var first = context.ForkRanks.OrderBy(x => x.Delay).First();
                Assert.Equal(0, first.Delay);
                Assert.Equal(ForkRankMode.Default, first.Mode);
                var second = context.ForkRanks.OrderBy(x => x.Delay).Skip(1).First();
                Assert.Equal(10000, second.Delay);
                Assert.Equal(ForkRankMode.Next, second.Mode);
                var third = context.ForkRanks.OrderBy(x => x.Delay).Skip(2).First();
                Assert.Equal(20000, third.Delay);
                Assert.Equal(ForkRankMode.Next, third.Mode);
            }
        }

        [Fact]
        public async Task ForkRankDelayIsUpdatedAsDelta()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 30, Number = "2005", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 40, Number = "2002", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {Id = 10, ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default, Delay = 500});
                context.ForkRanks.Add(new ForkRank {Id = 20, ExtensionId = 10, Index = 1, Mode = ForkRankMode.Default, Delay = 1000});
                context.ForkRanks.Add(new ForkRank {Id = 30, ExtensionId = 10, Index = 2, Mode = ForkRankMode.Default, Delay = 2000});
                context.SaveChanges();
            }
            
            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC Service Hotline\", \"number\": \"2000\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\":0}," +
                                                                                                                     "{\"extension\": \"2005\", \"active\": true, \"delay\":10}," +
                                                                                                                     "{\"extension\": \"2002\", \"active\": true, \"delay\":30}]}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var first = context.ForkRanks.OrderBy(x => x.Delay).First();
                Assert.Equal(0, first.Delay);
                Assert.Equal(ForkRankMode.Default, first.Mode);
                var second = context.ForkRanks.OrderBy(x => x.Delay).Skip(1).First();
                Assert.Equal(10000, second.Delay);
                Assert.Equal(ForkRankMode.Next, second.Mode);
                var third = context.ForkRanks.OrderBy(x => x.Delay).Skip(2).First();
                Assert.Equal(20000, third.Delay);
                Assert.Equal(ForkRankMode.Next, third.Mode);
            }
        }

        [Fact]
        public async Task UpdatGroupSwitchesFromSimpleToMultiring()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "4502", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC zivillian\", \"number\": \"4502\", \"extensions\": [" +
                                                                                                                     "{\"extension\": \"2001\", \"active\": true, \"delay\": 0}]}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single(x => x.Number == "4502");
                Assert.Equal(ExtensionType.Multiring, extension.Type);
                Assert.Single(context.ForkRanks);
                var member = Assert.Single(context.ForkRankMember);
                Assert.Equal(20, member.ExtensionId);
            }
        }

        [Fact]
        public async Task UpdatGroupSwitchesMultiringToSimple()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "4502", Type = ExtensionType.Multiring});
                context.Extensions.Add(new Extension {Id = 20, Number = "2001", Type = ExtensionType.Simple});
                context.ForkRanks.Add(new ForkRank {ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.ForkRankMember.Add(new ForkRankMember
                    {ExtensionId = 20, ForkRankId = 20, IsActive = true, Type = ForkRankMemberType.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_CALLGROUP", Data = JsonConvert.DeserializeObject<GuruData>("{\"name\": \"PoC zivillian\", \"number\": \"4502\", \"extensions\": []}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single(x => x.Number == "4502");
                Assert.Equal(ExtensionType.Simple, extension.Type);
                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithoutMultiringSwitchesToSimple()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-sip", Guru3Identifier = "SIP"});
                context.Extensions.Add(new Extension {Id = 10, Number = "4502", Type = ExtensionType.Multiring});
                context.ForkRanks.Add(new ForkRank {ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4502\", \"name\": \"PoC zivillian\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"call_waiting\": false, \"allow_dialout\": true, \"type\": \"SIP\", \"password\": \"96625287\"}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal(ExtensionType.Simple, extension.Type);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithMultiringKeepsMultiring()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-sip", Guru3Identifier = "SIP"});
                context.Extensions.Add(new Extension {Id = 10, Number = "4502", Type = ExtensionType.Multiring});
                context.ForkRanks.Add(new ForkRank {ExtensionId = 10, Index = 0, Mode = ForkRankMode.Default});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4502\", \"name\": \"PoC zivillian\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"call_waiting\": false, \"allow_dialout\": true, \"has_multiring\": true, \"type\": \"SIP\", \"password\": \"96625287\"}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal(ExtensionType.Multiring, extension.Type);
                Assert.Single(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithMultiringSwitchesToMultiring()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Hostname = "yate-sip", Guru3Identifier = "SIP"});
                context.Extensions.Add(new Extension {Id = 10, Number = "4502", Type = ExtensionType.Simple});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4502\", \"name\": \"PoC zivillian\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"call_waiting\": false, \"allow_dialout\": true, \"has_multiring\": true, \"type\": \"SIP\", \"password\": \"96625287\"}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal(ExtensionType.Multiring, extension.Type);
                Assert.Empty(context.ForkRanks);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithForwardingUpdatesForwarding()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group});
                context.Extensions.Add(new Extension {Id = 20, Number = "4502", Type = ExtensionType.Simple});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"2000\", \"name\": \"PoC Service Hotline\", \"location\": \"PoC\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"type\": \"GROUP\", \"forward_extension\": \"4502\", \"forward_delay\":10, \"forward_mode\":\"ON_BUSY\"}")};

            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single(x => x.Id == 10);
                Assert.Equal(ForwardingMode.OnBusy, extension.ForwardingMode);
                Assert.Equal(20, extension.ForwardingExtensionId);
                Assert.Equal(10000, extension.ForwardingDelay);
            }
        }

        [Fact]
        public async Task UpdateExtensionWithoutForwardingRemovesForwarding()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Extensions.Add(new Extension {Id = 20, Number = "4502", Type = ExtensionType.Simple});
                context.Extensions.Add(new Extension {Id = 10, Number = "2000", Type = ExtensionType.Group, ForwardingExtensionId = 20, ForwardingMode = ForwardingMode.OnBusy, ForwardingDelay = 10000});
                context.SaveChanges();
            }

            await service.ProcessMessageAsync(_updateGroupMessage, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = context.Extensions.Single(x => x.Id == 10);
                Assert.Equal(ForwardingMode.Disabled, extension.ForwardingMode);
                Assert.Null(extension.ForwardingExtensionId);
                Assert.Null(extension.ForwardingDelay);
            }
        }

        [Fact]
        public async Task RenameExtension()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Guru3Identifier = "SIP"});
                context.Extensions.Add(new Extension {Id = 10, Number = "4503", YateId = 1});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "RENAME_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"old_extension\":\"4503\",\"new_extension\":\"4502\"}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal("4502", extension.Number);
                Assert.Equal(10, extension.Id);
            }
        }

        [Fact]
        public async Task Trunk()
        {
            var service = GetService();
            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                context.Yates.Add(new Yate {Id = 1, Guru3Identifier = "SIP"});
                context.SaveChanges();
            }

            var guruData = new GuruMessage{Type = "UPDATE_EXTENSION", Data = JsonConvert.DeserializeObject<GuruData>("{\"number\": \"4503\", \"name\": \"\", \"location\": \"\", \"announcement_lang\": \"de-DE\", \"ringback_tone\": \"\", \"call_waiting\": true, \"allow_dialout\": false, \"trunk\": true, \"type\": \"SIP\", \"password\": \"PRQ0Y8lX\", \"isPremium\": false}")};
            await service.ProcessMessageAsync(guruData, CancellationToken.None);

            using (var context = new YwsdDbContext(_ywsdDbOptions))
            {
                var extension = Assert.Single(context.Extensions);
                Assert.Equal(ExtensionType.Trunk, extension.Type);
                Assert.Equal(1, extension.YateId);
            }
        }

        private YwsdSyncService GetService([CallerMemberName] string caller = "")
        {
            _ywsdDbOptions = new DbContextOptionsBuilder<YwsdDbContext>()
                .UseInMemoryDatabase(databaseName: caller)
                .Options;
            return new YwsdSyncService(NullLogger<YwsdSyncService>.Instance, new YwsdDbContext(_ywsdDbOptions));
        }
    }
}