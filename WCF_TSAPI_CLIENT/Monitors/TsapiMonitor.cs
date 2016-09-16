using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using log4net;
using WCF_TSAPI_CLIENT.TsapiServer;

namespace WCF_TSAPI_CLIENT.Monitors
{
    public class TsapiMonitor
    { 

        private static readonly ILog Log = LogManager.GetLogger(typeof(TsapiMonitor));

        private uint _idMonitor;

        public delegate void SetEventCollect(
            object mon, string evnt, int callid, CSTAEventCause_t cause = CSTAEventCause_t.ecNone);

        public event SetEventCollect OnSetEventCollect;

        public TsapiMonitor(uint idMonitor)
        {
            _idMonitor = idMonitor;
        }


        private void Debug(string evnt, int callid, CSTAEventCause_t cause = CSTAEventCause_t.ecNone)
        {
            var handler = OnSetEventCollect;
            if (handler != null)
            {
                handler.Invoke(this, evnt, callid, cause);
            }
            else
            {
                Log.Debug($"[{_idMonitor}] event {evnt} cause {cause}");
            }
        }

        internal void CopyEvent(Monitor monitor)
        {
            Debug("Copy Event",-1, CSTAEventCause_t.ecNone);
            var last = monitor.Mon;
            foreach (var eventInfo in last.GetType().GetEvents())
            {
                var fieldInfo = last.GetType()
                    .GetField(eventInfo.Name, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                var ev = fieldInfo?.GetValue(last);
                var ll = ev?.GetType().GetMethod("GetInvocationList").Invoke(ev, null) as Delegate[];
                if (ll == null) continue;
                foreach (var @delegate in ll)
                {
                    Debug($"Copy Event { @delegate.GetType().Name}",-1);
                    eventInfo.RemoveMethod.Invoke(last, new object[] { @delegate });
                    eventInfo.AddMethod.Invoke(this, new object[] { @delegate });
                }
            }
        }

        public delegate void CallCleared(CSTACallClearedEvent_t csta, ATTCallClearedEvent_t att);
        public delegate void Conferenced(CSTAConferencedEvent_t csta, ATTConferencedEvent_t att);
        public delegate void ConnectionCleared(CSTAConnectionClearedEvent_t csta);
        public delegate void Delivered(CSTADeliveredEvent_t csta, ATTDeliveredEvent_t att);
        public delegate void Diverted(CSTADivertedEvent_t csta, ATTDivertedEvent_t att);
        public delegate void Established(CSTAEstablishedEvent_t csta, ATTEstablishedEvent_t att);
        public delegate void Failed(CSTAFailedEvent_t csta, ATTFailedEvent_t att);
        public delegate void Held(CSTAHeldEvent_t csta);
        public delegate void NetworkReached(CSTANetworkReachedEvent_t csta, ATTNetworkReachedEvent_t att);
        public delegate void Originated(CSTAOriginatedEvent_t csta, ATTOriginatedEvent_t att);
        public delegate void Queued(CSTAQueuedEvent_t csta, ATTQueuedEvent_t att);
        public delegate void Retrieved(CSTARetrievedEvent_t csta);
        public delegate void ServiceInitiated(CSTAServiceInitiatedEvent_t csta, ATTServiceInitiatedEvent_t att);
        public delegate void Transferred(CSTATransferredEvent_t csta, ATTTransferredEvent_t att);
        public delegate void CallInformation(CSTACallInformationEvent_t csta);
        public delegate void DoNotDisturb(CSTADoNotDisturbEvent_t csta);
        public delegate void Forwarding(CSTAForwardingEvent_t csta);
        public delegate void MessageWaiting(CSTAMessageWaitingEvent_t csta);
        public delegate void BackInService(CSTABackInServiceEvent_t csta);
        public delegate void OutOfService(CSTAOutOfServiceEvent_t csta);
        public delegate void PrivateStatus(CSTAPrivateStatusEvent_t csta);
        public delegate void MonitorEnded(CSTAMonitorEndedEvent_t csta);
        public delegate void OnLoginDeviceDelegate(CSTALoggedOnEvent_t csta);
        public delegate void OnLogoutDeviceDelegate(CSTALoggedOffEvent_t csta);
        public delegate void ReadDelegate(OnReady csta);
        public delegate void LogoutDelegate(OnLogout csta);
        public delegate void LoginDelegate(OnLogin csta);
        public delegate void AftCallDelegate(OnAftCall csta);
        public delegate void NotReadyDelegate(OnNotReady csta);
       
        


        // ReSharper disable EventNeverSubscribedTo.Global
        public event ReadDelegate OnReadyEvent;
        public event LogoutDelegate OnLogoutEvent;
        public event LoginDelegate OnLoginEvent;
        public event AftCallDelegate OnAftCallEvent;
        public event NotReadyDelegate NotReadyEvent;
        public event CallCleared OnCallClearedEvent;
        public event Conferenced OnConferencedEvent;
        public event ConnectionCleared OnConnectionClearedEvent;
        public event Delivered OnDeliveredEvent;
        public event Diverted OnDivertedEvent;
        public event Established OnEstablishedEvent;
        public event Failed OnFailedEvent;
        public event Held OnHeldEvent;
        public event NetworkReached OnNetworkReachedEvent;
        public event Originated OnOriginatedEvent;
        public event Queued OnQueuedEvent;
        public event Retrieved OnRetrievedEvent;
        public event ServiceInitiated OnServiceInitiatedEvent;
        public event Transferred OnTransferredEvent;
        public event CallInformation OnCallInformationEvent;
        public event DoNotDisturb OnDoNotDisturbEvent;
        public event Forwarding OnForwardingEvent;
        public event MessageWaiting OnMessageWaitingEvent;
        public event BackInService OnBackInServiceEvent;
        public event OutOfService OnOutOfServiceEvent;
        public event PrivateStatus OnPrivateStatusEvent;
        public event MonitorEnded OnMonitorEnd;
        public event OnLogoutDeviceDelegate OnLogoutDevice;
        public event OnLoginDeviceDelegate OnLoginDevice;

        public uint IdMonitor
        {
            get { return _idMonitor; }
            set { _idMonitor = value; }
        }


        // ReSharper restore EventNeverSubscribedTo.Global


        



        public void CallClearedCall(CSTACallClearedEvent_t csta, ATTCallClearedEvent_t att)
        {
            Debug("CallClearedCall", csta.clearedCall.callID,csta.cause);
            OnCallClearedEvent?.Invoke(csta, att);
        }
        
        public void ConferencedCall(CSTAConferencedEvent_t csta, ATTConferencedEvent_t att)
        {
            Debug("ConferencedCall", csta.primaryOldCall.callID, csta.cause);
            OnConferencedEvent?.Invoke(csta, att);
        }

        public void ConnectionClearedCall(CSTAConnectionClearedEvent_t csta)
        {
            Debug("ConnectionClearedCall", csta.droppedConnection.callID, csta.cause);
            OnConnectionClearedEvent?.Invoke(csta);
        }

        public void DeliveredCall(CSTADeliveredEvent_t csta, ATTDeliveredEvent_t att)
        {
            Debug("DeliveredCall", csta.connection.callID, csta.cause);
            OnDeliveredEvent?.Invoke(csta, att);
        }

        public void DivertedCall(CSTADivertedEvent_t csta, ATTDivertedEvent_t att)
        {
            Debug("DivertedCall", csta.connection.callID, csta.cause);
            OnDivertedEvent?.Invoke(csta, att);
        }

        public void EstablishedCall(CSTAEstablishedEvent_t csta, ATTEstablishedEvent_t att)
        {
            Debug("EstablishedCall", csta.establishedConnection.callID, csta.cause);
            OnEstablishedEvent?.Invoke(csta, att);
        }

        public void FailedCall(CSTAFailedEvent_t csta, ATTFailedEvent_t att)
        {
            Debug("FailedCall", csta.failedConnection.callID, csta.cause);
            OnFailedEvent?.Invoke(csta, att);
        }

        public void HeldCall(CSTAHeldEvent_t csta)
        {
            Debug("HeldCall", csta.heldConnection.callID, csta.cause);
            OnHeldEvent?.Invoke(csta);
        }

        public void NetworkReachedCall(CSTANetworkReachedEvent_t csta, ATTNetworkReachedEvent_t att)
        {
            Debug("NetworkReachedCall", csta.connection.callID, csta.cause);
            OnNetworkReachedEvent?.Invoke(csta, att);
        }

        public void OriginatedCall(CSTAOriginatedEvent_t csta, ATTOriginatedEvent_t att)
        {
            Debug("OriginatedCall", csta.originatedConnection.callID, csta.cause);
            OnOriginatedEvent?.Invoke(csta, att);
        }

        public void QueuedCall(CSTAQueuedEvent_t csta, ATTQueuedEvent_t att)
        {
            Debug("QueuedCall", csta.queuedConnection.callID, csta.cause);
            OnQueuedEvent?.Invoke(csta, att);
        }

        public void RetrievedCall(CSTARetrievedEvent_t csta)
        {
            Debug("RetrievedCall", csta.retrievedConnection.callID, csta.cause);
            OnRetrievedEvent?.Invoke(csta);
        }

        public void ServiceInitiatedCall(CSTAServiceInitiatedEvent_t csta, ATTServiceInitiatedEvent_t att)
        {
            Debug("ServiceInitiatedCall", csta.initiatedConnection.callID, csta.cause);
            OnServiceInitiatedEvent?.Invoke(csta, att);
        }

        public void TransferredCall(CSTATransferredEvent_t csta, ATTTransferredEvent_t att)
        {
            Debug("TransferredCall", csta.primaryOldCall.callID, csta.cause);
            OnTransferredEvent?.Invoke(csta, att);
        }

        public void CallInformationCall(CSTACallInformationEvent_t csta)
        {
            Debug("CallInformationCall", csta.connection.callID);
            OnCallInformationEvent?.Invoke(csta);
        }

        public void DoNotDisturbCall(CSTADoNotDisturbEvent_t csta)
        {
            Debug("DoNotDisturbCall", 0);
            OnDoNotDisturbEvent?.Invoke(csta);
        }

        public void ForwardingCall(CSTAForwardingEvent_t csta)
        {
            Debug("ForwardingCall", 0);
            OnForwardingEvent?.Invoke(csta);
        }

        public void MessageWaitingCall(CSTAMessageWaitingEvent_t csta)
        {
            Debug("MessageWaitingCall", 0);
            OnMessageWaitingEvent?.Invoke(csta);
        }

        public void BackInServiceCall(CSTABackInServiceEvent_t csta)
        {
            Debug("BackInServiceCall", 0, csta.cause);
            OnBackInServiceEvent?.Invoke(csta);
        }

        public void OutOfServiceCall(CSTAOutOfServiceEvent_t csta)
        {
            Debug("OutOfServiceCall", 0, csta.cause);
            OnOutOfServiceEvent?.Invoke(csta);
        }

        public void PrivateStatusCall(CSTAPrivateStatusEvent_t csta)
        {
            Debug("PrivateStatusCall", 0);
            OnPrivateStatusEvent?.Invoke(csta);
        }

        public void MonitorEnd(CSTAMonitorEndedEvent_t csta, OnMonitorEnded onMonitorEnded)
        {
            Debug("MonitorEndedCall", 0, csta.cause);
            OnMonitorEnd?.Invoke(csta);
        }

        public void LogoutDevice(CSTALoggedOffEvent_t csta)
        {
            Debug("OnLogoutDevice", 0);
            OnLogoutDevice?.Invoke(csta);
        }

        public void LoginDevice(CSTALoggedOnEvent_t csta)
        {
            Debug("OnLoginDevice", 0);
            OnLoginDevice?.Invoke(csta);
        }


        public void Ready(OnReady arg)
        {
            Debug("OnReady", 0);
            OnReadyEvent?.Invoke(arg);
        }

        public void Logout(OnLogout arg)
        {
            Debug("OnLogoutEvent", 0);
            OnLogoutEvent?.Invoke(arg);
        }

        public void Login(OnLogin arg)
        {
            Debug("OnLoginEvent", 0);
            OnLoginEvent?.Invoke(arg);
        }

        public void AftCall(OnAftCall arg)
        {
            Debug("OnAftCallEvent", 0);
            OnAftCallEvent?.Invoke(arg);
        }

        public void NotReady(OnNotReady arg)
        {
            Debug("NotReadyEvent", 0);
            NotReadyEvent?.Invoke(arg);
        }


     
    }


}