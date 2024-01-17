using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Omm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mitelapi;
using mitelapi.Types;

namespace epmgr.Services
{
    public class OmmSyncService : IGuruMessageHandler,IExtensionService
    {
        private readonly ILogger<OmmSyncService> _logger;
        private readonly IOptions<Settings> _options;
        private readonly IOmmClient _ommClient;
        private readonly RandomPasswordService _random;
        private readonly IServiceProvider _provider;

        public int Order => 40;

        public bool IsThreadSafe => false;

        public OmmSyncService(ILogger<OmmSyncService> logger, IOptions<Settings> options, IOmmClient ommClient, RandomPasswordService random, IServiceProvider provider)
        {
            _logger = logger;
            _options = options;
            _ommClient = ommClient;
            _random = random;
            _provider = provider;
        }

        public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
        {
            switch (guruMessage.Type)
            {
                case GuruMessageType.UpdateExtension:
                    return CreateExtension(MgrCreateExtension.Create(guruMessage.Data), cancellationToken);
                case GuruMessageType.DeleteExtension:
                    return DeleteExtension(guruMessage.Data.Number, cancellationToken);
                case GuruMessageType.RenameExtension:
                    return RenameExtension(guruMessage.Data.OldExtension, guruMessage.Data.NewExtension, cancellationToken);
                case GuruMessageType.UnsubscribeDevice:
                    return DeleteExtension(guruMessage.Data.Extension, cancellationToken);
                case GuruMessageType.StartResync:
                case GuruMessageType.EndResync:
                case GuruMessageType.UpdateGroup:
                    return Task.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException(nameof(guruMessage.Type), guruMessage.Type, "Unsupported GURU3 message type");
            }
        }

        private async Task RenameExtension(string oldExtension, string newExtension, CancellationToken cancellationToken)
        {
            try
            {
                var ommUser = await _ommClient.GetPPUserByNumberAsync(oldExtension, cancellationToken);
                var ppUser = new PPUserType
                {
                    Uid = ommUser.Uid,
                    Num = newExtension,
                    AddId = newExtension,
                    SipAuthId = newExtension,
                };

                await _ommClient.SetPPUserAsync(ppUser, cancellationToken);
            }
            catch (OmmNoEntryException)
            {
                //nothing to rename - ignore
                return;
            }
        }

        public async Task DeleteExtension(string extension, CancellationToken cancellationToken)
        {
            try
            {
                var ommUser = await _ommClient.GetPPUserByNumberAsync(extension, cancellationToken);

                _logger.LogInformation("Deleting extension '{0}'", extension);
                await _ommClient.DeletePPUserAsync(ommUser.Uid, cancellationToken);
                if (ommUser.Ppn != 0)
                {
                    var tmpUser = await CreateTempUserProfile(cancellationToken);
                    await AttachDevice(tmpUser.Uid, ommUser.Ppn, cancellationToken);
                }

            }
            catch (OmmNoEntryException)
            {
                //nothing to do if it doesn't exist
            }
        }

        public async Task CreateExtension(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            if (extension.Type != MgrExtensionType.DECT) return;
            PPUserType ommUser;
            var dect = extension.DectDevice;
            try
            {
                ommUser = await _ommClient.GetPPUserByNumberAsync(extension.Number, cancellationToken);
            }
            catch (OmmNoEntryException)
            {
                if (dect is null)
                    return;
                ommUser = await CreateUserProfile(extension.Name, extension.Number, extension.Password, cancellationToken);
            }
            
            if (!(dect is null))
            {
                PPDevType device;
                try
                {
                    device = await _ommClient.GetPPDevByIPEIAsync(dect.Ipei, cancellationToken);
                    await _ommClient.SetPPDevAsync(new PPDevType { Ppn = device.Ppn, AutoCreate = false }, cancellationToken);
                }
                catch (OmmNoEntryException)
                {
                    device = new PPDevType
                    {
                        Ipei = dect.Ipei,
                        Uak = dect.Uak,
                        AutoCreate = false,
                        RelType = PPRelTypeType.Unbound,
                        SubscriptionState = DECTSubscriptionStateType.Yes
                    };
                    device = await _ommClient.CreatePPDevAsync(device, cancellationToken);
                }
                if (device.Uid.HasValue && device.Uid != 0 && device.Uid != ommUser.Uid)
                {
                    var oldUser = await _ommClient.GetPPUserAsync(device.Uid.Value, cancellationToken);
                    await _ommClient.DeletePPUserAsync(device.Uid.Value, cancellationToken);
                    if (oldUser.Num.StartsWith(Settings.DectTempPrefix))
                    {
                        using (var scope = _provider.CreateScope())
                        {
                            var extensionServices = scope.ServiceProvider.GetService<IEnumerable<IExtensionService>>()?.ToList();
                            if (!(extensionServices is null))
                            {
                                _logger.LogInformation("Triggering delete for '{0}'", oldUser.Num);
                                var tasks = extensionServices.Where(x => !(x is OmmSyncService))
                                    .Select(x => x.DeleteExtension(oldUser.Num, cancellationToken));
                                await Task.WhenAll(tasks);
                            }
                        }
                    }
                }
                else if (ommUser.Ppn != 0 && ommUser.Ppn != device.Ppn)
                {
                    await DetachDevice(ommUser.Uid, ommUser.Ppn, cancellationToken);
                    var tempUser = await CreateTempUserProfile(cancellationToken);
                    await AttachDevice(tempUser.Uid, ommUser.Ppn, cancellationToken);
                }
                if (device.Uid != ommUser.Uid)
                {
                    await AttachDevice(ommUser.Uid, device.Ppn, cancellationToken);
                }
                await _ommClient.SetPPDevAsync(new PPDevType {Ppn = device.Ppn, AutoCreate = true}, cancellationToken);
                ommUser.Ppn = device.Ppn;
            }
            _logger.LogInformation("Updating extension '{0}'", extension.Number);
            await UpdateUserProfile(ommUser.Uid, extension.Name, cancellationToken);
            await SetEncryption(ommUser.Ppn, extension.Encryption, cancellationToken);
        }

        private Task<PPUserType> UpdateUserProfile(int uid, string name, CancellationToken cancellationToken)
        {
            var user = new PPUserType{Uid = uid, Name = name};
            if (user.Name?.Length > 20)
            {
                user.Name = user.Name.Substring(0, 20);
            }
            _logger.LogInformation("Setting name for User '{0}' to '{1}'", uid, name);
            return _ommClient.SetPPUserAsync(user, cancellationToken);
        }

        public Task SetEncryption(int ppn, bool useEncryption, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Setting encryption for Device '{0}' to '{1}'", ppn, useEncryption);
            return _ommClient.SetPPDevAsync(new PPDevType {Encrypt = useEncryption, Ppn = ppn}, cancellationToken);
        }

        public async Task<PPUserType> CreateTempUserProfile(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating Temp extension");
            var rnd = new Random();
            string extension;
            while (true)
            {
                extension = Settings.DectTempPrefix + rnd.Next(1000, 10000);
                try
                {
                    await _ommClient.GetPPUserByNumberAsync(extension, cancellationToken);
                }
                catch (OmmNoEntryException)
                {
                    //found unused extension
                    break;
                }
            }
            var password = _random.GenerateSipPassword();
            var user = await CreateUserProfile("DECT Temp", extension, password, cancellationToken);
            
            _logger.LogInformation("Trigger extension creation for '{0}'", extension);
            using (var scope = _provider.CreateScope())
            {
                var extensionServices = scope.ServiceProvider.GetService<IEnumerable<IExtensionService>>()?.ToList();
                if (!(extensionServices is null))
                {
                    var mgrExtension = new MgrCreateExtension
                    {
                        Number = extension,
                        Name = user.Name,
                        Type = MgrExtensionType.DECT,
                        Password = password,
                        DisplayModus = DectDisplayModus.NumName,
                        Language = _options.Value.DefaultLanguage
                    };
                    _logger.LogInformation("Triggering create for '{0}'", mgrExtension.Number);
                    var tasks = extensionServices.Where(x=>!(x is OmmSyncService))
                        .Select(x => x.CreateExtension(mgrExtension, cancellationToken));
                    await Task.WhenAll(tasks);
                }
            }
            return user;
        }

        public async Task<PPUserType> CreateUserProfile(string name, string extension, string password, CancellationToken cancellationToken)
        {
            if (name.Length > 20)
                name = name.Substring(0, 20);
            var user = new PPUserType
            {
                Name = name,
                Num = extension,
                AddId = extension,
                Pin = _random.GenerateDectPin(),
                SipAuthId = extension,
                SipPw = password
            };
            _logger.LogInformation("Creating PPUser for '{0}'", extension);
            var response = await _ommClient.CreatePPUserAsync(user, cancellationToken);

            return response;
        }

        public Task AttachDevice(int uid, int ppn, CancellationToken cancellationToken)
        {
            var device = new PPDevType {Ppn = ppn, RelType = PPRelTypeType.Dynamic, Uid = uid};
            var user = new PPUserType { Uid = uid, RelType = PPRelTypeType.Dynamic, Ppn = ppn };
            _logger.LogInformation("Attaching Device '{0}' to User '{1}'", ppn, uid);
            return _ommClient.SetPPAsync(device, user, cancellationToken);
        }

        public Task DetachDevice(int uid, int ppn, CancellationToken cancellationToken)
        {
            var pp = new PPDevType()
            {
                Uid = 0,
                RelType = PPRelTypeType.Unbound,
                Ppn = ppn
            };
            var user = new PPUserType()
            {
                Ppn = 0,
                Uid = uid,
                RelType = PPRelTypeType.Unbound
            };
            return _ommClient.SetPPAsync(pp, user, CancellationToken.None);
        }
    }
}