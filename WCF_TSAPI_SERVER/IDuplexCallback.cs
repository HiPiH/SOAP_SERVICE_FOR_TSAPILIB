using System;
using System.ServiceModel;
using TSAPILIB2;

namespace WCF_TSAPI_SERVER
{
    public interface IDuplexCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnReady(String agent, ATTQueryAgentStateConfEvent_t arg, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnNotReady(String agent, ATTQueryAgentStateConfEvent_t arg, uint monId);    
        [OperationContract(IsOneWay = true)]
        void OnAftCall(String agent, ATTQueryAgentStateConfEvent_t arg, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnLogin(String agent, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnLoginDevice(CSTALoggedOnEvent_t arg, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnLogout(String agent, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnLogoutDevice(CSTALoggedOffEvent_t agent,uint monId );
        [OperationContract(IsOneWay = true)]
        void OnCallCleared(String device, CSTACallClearedEvent_t csta, ATTCallClearedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnConferenced(String device, CSTAConferencedEvent_t csta, ATTConferencedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnConnectionCleared(String device, CSTAConnectionClearedEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnDelivered(String device, CSTADeliveredEvent_t csta, ATTDeliveredEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnDiverted(String device, CSTADivertedEvent_t csta, ATTDivertedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnEstablished(String device, CSTAEstablishedEvent_t csta, ATTEstablishedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnFailed(String device, CSTAFailedEvent_t csta, ATTFailedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnHeld(String device, CSTAHeldEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnNetworkReached(String device, CSTANetworkReachedEvent_t csta, ATTNetworkReachedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnOriginated(String device, CSTAOriginatedEvent_t csta, ATTOriginatedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnQueued(String device, CSTAQueuedEvent_t csta, ATTQueuedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnRetrieved(String device, CSTARetrievedEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnServiceInitiated(String device, CSTAServiceInitiatedEvent_t csta, ATTServiceInitiatedEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnTransferred(String device, CSTATransferredEvent_t csta, ATTTransferredEvent_t att, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnCallInformation(String device, CSTACallInformationEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnDoNotDisturb(String device, CSTADoNotDisturbEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnForwarding(String device, CSTAForwardingEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnMessageWaiting(String device, CSTAMessageWaitingEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnBackInService(String device, CSTABackInServiceEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnOutOfService(String device, CSTAOutOfServiceEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnPrivateStatus(String device, CSTAPrivateStatusEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void OnMonitorEnded(String device, CSTAMonitorEndedEvent_t csta, uint monId);
        [OperationContract(IsOneWay = true)]
        void ServerFail();
    }
}