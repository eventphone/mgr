using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using mitelapi;
using mitelapi.Events;
using mitelapi.Messages;
using mitelapi.Types;

namespace epmgr.Omm
{
    public interface IOmmClient:IDisposable
    {
        Task<PPUserType> GetPPUserAsync(int uid, CancellationToken cancellationToken);

        Task<PPUserType> GetPPUserByNumberAsync(string num, CancellationToken cancellationToken);

        Task DeletePPUserAsync(int uid, CancellationToken cancellationToken);

        Task<PPUserType> CreatePPUserAsync(PPUserType user, CancellationToken cancellationToken);

        Task<SetPPResp> SetPPAsync(PPDevType pp, PPUserType user, CancellationToken cancellationToken);

        Task<PPDevType> SetPPDevAsync(PPDevType pp, CancellationToken cancellationToken);

        Task<PPUserType> SetPPUserAsync(PPUserType user, CancellationToken cancellationToken);

        Task<PPDevType> CreatePPDevAsync(PPDevType pp, CancellationToken cancellationToken);

        Task<PPDevType> GetPPDevAsync(int ppn, CancellationToken cancellationToken);

        Task<PPDevType> GetPPDevByIPEIAsync(string ipei, CancellationToken cancellationToken);

        Task<List<RFPType>> GetRFPAllAsync(bool withDetails, bool withState, CancellationToken cancellationToken);

        Task SetRFPAsync(RFPType rfp, CancellationToken cancellationToken);

        Task<List<PPUserType>> GetPPAllUserAsync(CancellationToken cancellationToken);

        Task<DECTSubscriptionModeType> GetDECTSubscriptionModeAsync(CancellationToken cancellationToken);

        Task<bool> GetDevAutoCreateAsync(CancellationToken cancellationToken);

        Task<bool> GetRFPCaptureAsync(CancellationToken cancellationToken);

        Task<SetDevAutoCreateResp> SetDevAutoCreateAsync(bool enabled, CancellationToken cancellationToken);

        Task<SetDECTSubscriptionModeResp> SetDECTSubscriptionModeAsync(DECTSubscriptionModeType mode, CancellationToken cancellationToken);

        Task<SetRFPCaptureResp> SetRFPCaptureAsync(bool enabled, CancellationToken cancellationToken);

        Task LoginAsync(string username, string password, bool userDeviceSync = false, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<PPDevType>> GetPPAllDevAsync(CancellationToken cancellationToken);

        Task DeletePPDevAsync(int ppn, CancellationToken cancellationToken);
        
        Task SubscribeAsync(SubscribeCmdType[] commands, CancellationToken cancellationToken);

        Task<PPDevType[]> GetPPDevAsync(int ppn, int maxRecords, CancellationToken cancellationToken);

        Task<GetRFPCaptureListResp> GetRFPCaptureListAsync(CancellationToken cancellationToken);

        Task<DeleteRFPCaptureListElemResp> DeleteRFPCaptureListElemAsync(string mac, CancellationToken cancellationToken);

        Task<RFPType> CreateRFPAsync(RFPType rfp, CancellationToken cancellationToken);

        Task<GetRFPSyncResp> GetRFPSyncAsync(int id, CancellationToken cancellationToken);

        Task<GetPPStateResp> GetPPStateAsync(int ppn, CancellationToken cancellationToken);

        Task<RFPType> GetRFPAsync(int id, bool withDetails, bool withState, CancellationToken cancellationToken);

        Task DeleteRFPAsync(int id, CancellationToken cancellationToken);

        Task<RequestRFPEnrollmentResp> RequestRFPEnrollmentAsync(uint rfpId, CancellationToken cancellationToken);

        event EventHandler<LogMessageEventArgs> MessageLog;
        event EventHandler<OmmEventArgs<EventAlarmCallProgress>> AlarmCallProgress;
        event EventHandler<OmmEventArgs<EventDECTSubscriptionMode>> DECTSubscriptionModeChanged;
        event EventHandler<OmmEventArgs<EventPPCnf>> PPCnf;
        event EventHandler<OmmEventArgs<EventPPDevCnf>> PPDevCnf;
        event EventHandler<OmmEventArgs<EventPPDevSummary>> PPDevSummary;
        event EventHandler<OmmEventArgs<EventPPUserCnf>> PPUserCnf;
        event EventHandler<OmmEventArgs<EventPPUserSummary>> PPUserSummary;
        event EventHandler<OmmEventArgs<EventRFPState>> RFPState;
        event EventHandler<OmmEventArgs<EventRFPCnf>> RFPCnf;
        event EventHandler<OmmEventArgs<EventRFPSummary>> RfpSummary;
        event EventHandler<OmmEventArgs<EventRFPSyncQuality>> RFPSyncQuality;
        event EventHandler<OmmEventArgs<EventRFPSyncRel>> RFPSyncRel;
        event EventHandler<OmmEventArgs<EventStbStateChange>> StbStateChange;
        event EventHandler<OmmEventArgs<EventLicenseCnf>> LicenseCnf;
        event EventHandler<OmmEventArgs<EventMessageProgress>> MessageProgress;
        event EventHandler<OmmEventArgs<EventMessageConfirmation>> MessageConfirmation;
        event EventHandler<OmmEventArgs<EventMessageQueueEmpty>> MessageQueueEmpty;
        event EventHandler<OmmEventArgs<EventMessageSend>> MessageSend;
        event EventHandler<OmmEventArgs<EventPositionHistory>> PositionHistory;
        event EventHandler<OmmEventArgs<EventPositionInfo>> PositionInfo;
        event EventHandler<OmmEventArgs<EventPositionTrack>> PositionTrack;
        event EventHandler<OmmEventArgs<EventPositionRequest>> PositionRequest;
    }

    public class MitelClient : OmmClient, IOmmClient
    {
        public MitelClient(string hostname, int port = 12622) : base(hostname, port)
        {
        }

        protected override bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
