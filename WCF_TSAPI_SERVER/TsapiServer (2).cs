using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.ServiceModel;
using System.Threading.Tasks;
using TSAPILIB2;
using System.Threading;
using WCF_TSAPI_SERVER.Properties;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using log4net;

namespace WCF_TSAPI_SERVER
{
   

    [DataContract] 
    public class CSTAFail
    {
        [DataMember]
        public CSTAUniversalFailure_t CustomError; 
        public CSTAFail()
        {   
        }
        public CSTAFail(CSTAUniversalFailure_t error)
        {   
            CustomError = error;   
        }   
    }

    

    
    [DataContract] 
    public class SYSFail
    {
        [DataMember]
        public Exception CustomError; 
        public SYSFail()
        {   
        }
        public SYSFail(Exception error)
        {   
            CustomError = error;   
        }   
    }

     [DataContract] 
    public class ACSFail
    {
        [DataMember]
        public ACSFunctionRet_t CustomError;
        public ACSFail()
        {
        }
        public ACSFail(ACSFunctionRet_t error)
        {
            CustomError = error;
        }
    }

    public enum AgentStatus
    {
 
        Ready,
        NotReady,
        AftCall
    }
    
     public interface IDuplexCallback
     {
         [OperationContract(IsOneWay = true)]
         void OnReady(String agent,  ATTQueryAgentStateConfEvent_t arg );
         [OperationContract(IsOneWay = true)]
         void OnNotReady(String agent,  ATTQueryAgentStateConfEvent_t arg );    
         [OperationContract(IsOneWay = true)]
         void OnAftCall(String agent,  ATTQueryAgentStateConfEvent_t arg );
         [OperationContract(IsOneWay = true)]
         void OnLogin(String agent);
         [OperationContract(IsOneWay = true)]
         void OnLogout(String agent);
         [OperationContract(IsOneWay = true)]
         void OnCallCleared(String device, CSTACallClearedEvent_t csta, ATTCallClearedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnConferenced(String device, CSTAConferencedEvent_t csta, ATTConferencedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnConnectionCleared(String device, CSTAConnectionClearedEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnDelivered(String device, CSTADeliveredEvent_t csta, ATTDeliveredEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnDiverted(String device, CSTADivertedEvent_t csta, ATTDivertedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnEstablished(String device, CSTAEstablishedEvent_t csta, ATTEstablishedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnFailed(String device, CSTAFailedEvent_t csta, ATTFailedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnHeld(String device, CSTAHeldEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnNetworkReached(String device, CSTANetworkReachedEvent_t csta, ATTNetworkReachedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnOriginated(String device, CSTAOriginatedEvent_t csta, ATTOriginatedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnQueued(String device, CSTAQueuedEvent_t csta, ATTQueuedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnRetrieved(String device, CSTARetrievedEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnServiceInitiated(String device, CSTAServiceInitiatedEvent_t csta, ATTServiceInitiatedEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnTransferred(String device, CSTATransferredEvent_t csta, ATTTransferredEvent_t att, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnCallInformation(String device, CSTACallInformationEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnDoNotDisturb(String device, CSTADoNotDisturbEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnForwarding(String device, CSTAForwardingEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnMessageWaiting(String device, CSTAMessageWaitingEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnBackInService(String device, CSTABackInServiceEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnOutOfService(String device, CSTAOutOfServiceEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnPrivateStatus(String device, CSTAPrivateStatusEvent_t csta, String call = "");
         [OperationContract(IsOneWay = true)]
         void OnMonitorEnded(String device, CSTAMonitorEndedEvent_t csta, String call = "");


     }
     enum TypeMonitor
     {
         Device,
         DeviceVia,
         Call,
         Agent
     }
    
     struct DuplexMonitor
     {
        public ConnectionID_t Target;
        public IDuplexCallback Callback;
        public TypeMonitor Type;
     }

    [CallbackBehavior( ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext=true, IncludeExceptionDetailInFaults=true)]
    [ServiceContract(CallbackContract = typeof(IDuplexCallback), SessionMode=SessionMode.Required)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession, 
        IncludeExceptionDetailInFaults = true, UseSynchronizationContext = true)]
    class TsapiServer:IDisposable
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(TsapiServer)); 
        private ConcurrentDictionary<String, DuplexMonitor> _monitors = new ConcurrentDictionary<string, DuplexMonitor>();
        TSAPI _mainConnect;
        TSAPI _subConnect;
        readonly Object _mainConnectLocked = new object();
        readonly Object _subConnectLocked = new object();
        protected Boolean InWork = true;
        protected TSAPI Tsapi
        {
            get
            {
               
                    if (_mainConnect == null)
                    {
                        lock (_mainConnectLocked)
                        {
                            if (_mainConnect == null)
                            {
                                _mainConnect = new TSAPI(Settings.Default.server, Settings.Default.login,
                                    Settings.Default.password, GetType().Name + "_main", Settings.Default.api,
                                    Settings.Default.version);
                                _mainConnect.OnConnnected += main_connect_OnConnnected;
                                _mainConnect.OnClosed += main_connect_OnClosed;
                                _mainConnect.OpenStream();
                            }
                        }
                    }
                    if (SpinWait.SpinUntil(() => _mainConnect.StatusConnection == StatusConection.Open, 5000))
                    {
                        return _mainConnect;
                    }
                
                Log.DebugFormat("TSAPI main_connect  Curretn status {0}", _mainConnect.StatusConnection);
                throw  new Exception("TSAPI main connect no openned!");
            }
        }

        private Boolean AnalizError(Exception innerException, String message)
        {
                var csta = innerException as CSTAExeption;
                var asc = innerException as Exeption;
                Log.Error(message, innerException);
                if (csta != null) throw new FaultException<CSTAFail>(new CSTAFail(csta.Code), csta.Code.ToString());
                if (asc != null) throw new FaultException<ACSFail>(new ACSFail(asc.Code), asc.Code.ToString());
                throw new FaultException<SYSFail>(new SYSFail(innerException), innerException.ToString());
        }

        private Task<T> HuckForException<T>(Task<T> t, String message = "")
        {
            return t.ContinueWith(_ =>
            {
                if (_.Exception != null) _.Exception.Handle(e => AnalizError(e, message));
                return _.Result;
            },TaskContinuationOptions.NotOnRanToCompletion);
        }


        protected TSAPI Monitor
        {
            get
            {
                    if (_subConnect == null)
                    {
                        lock (_subConnectLocked)
                        {
                            if (_subConnect == null)
                            {
                                _subConnect = new TSAPI(Settings.Default.server, Settings.Default.login,
                                    Settings.Default.password, GetType().Name + "_monitor", Settings.Default.api,
                                    Settings.Default.version);
                                _subConnect.OnConnnected += main_connect_OnConnnected;
                                _subConnect.OnClosed += main_connect_OnClosed;
                                _subConnect.OpenStream();
                            }

                        }
                    }
                    if (SpinWait.SpinUntil(() => _subConnect.StatusConnection == StatusConection.Open, 5000))
                    {
                        return _mainConnect;
                    }
                    Log.DebugFormat("TSAPI sub_connect . Curretn status {0}", _mainConnect.LinkName);
                    throw new Exception("TSAPI sub connect  no openned!");
            }
        }
        ~TsapiServer()
        {
            Dispose();
        }
        public TsapiServer()
        {
            Log.Info(String.Format("TsapiServer instance start."));
        }

        protected void main_connect_OnClosed(object sender, EventArgs arg)
        {
            Contract.Assert(sender is TSAPI);
            var tsapi = sender as TSAPI;
            Log.Info(String.Format("TSAPI Connect {0} close.", tsapi.LinkName));
            if (!InWork) return;
            tsapi.OpenStream();
            if (sender != _subConnect) return;
            foreach (var mon in _monitors.Values)
            {
                if (mon.Type != TypeMonitor.Agent)
                {
                    switch (mon.Type)
                    {
                        case TypeMonitor.Call: setMonitorCall(mon.Target); break;
                        case TypeMonitor.Device: setMonitorDevice(mon.Target.deviceID); break;
                        case TypeMonitor.DeviceVia: setMonitorCallsViaDevice(mon.Target.deviceID); break;
                    }
                }
            }
        }

        protected void main_connect_OnConnnected(object sender, EventArgs  arg)
        {
            Log.Info(String.Format("TSAPI Connect {0} open.", ((TSAPI)sender).LinkName));
        }

        // ReSharper disable InconsistentNaming        
        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryDeviceInfoReturn> getQueryDeviceInfo(string device)
        {
            
            return HuckForException(Tsapi.GetQueryDeviceInfo(device), String.Format("getQueryDeviceInfo {0}", device));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryLastNumberEventReturn> getQueryLastNumber(string deviceID)
        {
            return HuckForException(Tsapi.GetQueryLastNumber(deviceID), String.Format("getQueryLastNumber {0}", deviceID));
         }


        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryAgentStateEventReturn> getQueryAgentState(string deviceID)
        {
            return HuckForException(Tsapi.GetQueryAgentState(deviceID), String.Format("getQueryAgentState {0}", deviceID));
        }



        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryMsgWaitingEventReturn> getQueryMsgWaitingInd(string deviceID)
        {
            return HuckForException(Tsapi.GetQueryMsgWaitingInd(deviceID), String.Format("getQueryMsgWaitingInd {0}", deviceID));
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryStationStatusEventReturn> getQueryStationStatus(string deviceID)
        {
            return HuckForException(Tsapi.GetQueryStationStatus(deviceID), String.Format("getQueryStationStatus {0}", deviceID));
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryUCIDEventReturn> getQueryUCID(ConnectionID_t call)
        {
            return HuckForException(Tsapi.GetQueryUCID(call), String.Format("getQueryUCID {0} {1} {2}", call.callID, call.deviceID, call.devIDType));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAgentState(string deviceID, string agentID, string agentGroup, string password, AgentMode_t mode, ATTWorkMode_t wmode, int reasonCode)
        {
             return HuckForException(Tsapi.SetAgentState(deviceID, agentID, agentGroup, password, mode, wmode, reasonCode),
                String.Format("setAgentState {0} {1} {2} {3}", deviceID, agentID, agentGroup, password));
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAlternateCall(ConnectionID_t activeCall, ConnectionID_t otherCall)
        {

            return HuckForException(Tsapi.SetAlternateCall(activeCall, otherCall),
             String.Format("setAlternateCall {0} {1} {2} {3}", activeCall.callID, activeCall.deviceID, otherCall.callID, otherCall.deviceID));
       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAnswerCall(ConnectionID_t allertingCall)
        {
            return HuckForException(Tsapi.SetAnswerCall(allertingCall),
             String.Format("getQueryDeviceInfo {0} {1}", allertingCall.callID, allertingCall.deviceID));
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setCallCompletion(ConnectionID_t call, Feature_t feature)
        {

            return HuckForException(Tsapi.SetCallCompletion(call, feature),
            String.Format("setCallCompletion {0} {1}", call.callID, call.deviceID));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ChangeMonitorFilterEventReturn> setChangeMonitorFilter(uint monitorCrossId, CSTAMonitorFilter_t filter)
        {
            return HuckForException(Tsapi.SetChangeMonitorFilter(monitorCrossId, filter),String.Format("setChangeMonitorFilter {0}", monitorCrossId));

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setClearCall(ConnectionID_t call)
        {
            return HuckForException(Tsapi.SetClearCall(call),
                String.Format("setClearCall {0} {1}", call.callID, call.deviceID));

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setClearConnection(ConnectionID_t call, ATTDropResource_t resourse, ATTV5UserToUserInfo_t info)
        {
            return HuckForException(Tsapi.SetClearConnection(call, resourse, info),
                String.Format("setClearConnection {0} {1}", call.callID, call.deviceID));

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ConferenceCallEventReturn> setConferenceCall(ConnectionID_t activeCall, ConnectionID_t otherCall)
        {
            return HuckForException(Tsapi.SetConferenceCall(activeCall, otherCall),
               String.Format("setConferenceCall {0} {1} {2} {3}", activeCall.callID, activeCall.deviceID, otherCall.callID, otherCall.deviceID));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ConsultationCallEventReturn> setConsultationCall(ConnectionID_t activeCall, string calledDevice, string destRoute, bool priorityCalling, ATTV5UserToUserInfo_t info)
        {
            return HuckForException(Tsapi.SetConsultationCall(activeCall, calledDevice, destRoute, priorityCalling, info),
             String.Format("setConsultationCall {0} {1} {2}", activeCall.callID, activeCall.deviceID, calledDevice));

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setDeflectCall(ConnectionID_t deflectCall, string calledDevice)
        {
            return HuckForException(Tsapi.SetDeflectCall(deflectCall, calledDevice),
             String.Format("setDeflectCall {0} {1} {2}", deflectCall.callID, deflectCall.deviceID, calledDevice));


         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setGroupPickupCall(ConnectionID_t deflectCall, string pickupDevice)
        {

            return HuckForException(Tsapi.SetGroupPickupCall(deflectCall, pickupDevice),
             String.Format("setGroupPickupCall {0} {1} {2}", deflectCall.callID, deflectCall.deviceID, pickupDevice));

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setHoldCall(ConnectionID_t activeCall)
        {
            return HuckForException(Tsapi.SetHoldCall(activeCall),
                String.Format("setHoldCall  {0}, {1}", activeCall.callID, activeCall.deviceID));

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<MakeCallEventReturn> setMakeCall(string callingDevice, string calledDevice, string destroute, bool priorityCall, ATTV5UserToUserInfo_t info)
        {
            return HuckForException(Tsapi.SetMakeCall(callingDevice, calledDevice, destroute, priorityCall, info),
                String.Format("setMakeCall {0}, {1}", callingDevice, calledDevice));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<MakePredictiveCallEventReturn> setMakePredictiveCall(string callingDevice, string calledDevice, AllocationState_t allocationState, string destRoute, bool priorityCalling, short maxRing, ATTAnswerTreat_t answerTreat, ATTV5UserToUserInfo_t info)
        {
            return HuckForException(Tsapi.SetMakePredictiveCall(callingDevice, calledDevice, allocationState, destRoute, priorityCalling, maxRing, answerTreat, info),
                String.Format("setMakePredictiveCall  {0}, {1}", callingDevice, calledDevice));
        }
      

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setMonitorStop(uint monitorCrossId)
        {
            return HuckForException(Tsapi.SetMonitorStop(monitorCrossId),
              String.Format("setMonitorStop {0}", monitorCrossId));

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setPickupCall(ConnectionID_t deflectCall, string calledDevice)
        {
            return HuckForException(Tsapi.SetPickupCall(deflectCall, calledDevice),
          String.Format("getQueryDeviceInfo {0} {1} {2}", deflectCall.callID, deflectCall.deviceID, calledDevice));

          }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryCallMonitorEventReturn> setQueryCallMonitor(string deviceID)
        {

            return HuckForException(Tsapi.SetQueryCallMonitor(deviceID),
        String.Format("setQueryCallMonitor {0}", deviceID));


       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setReconnectCall(ConnectionID_t activeCall, ConnectionID_t heldCall, ATTDropResource_t resource, ATTV5UserToUserInfo_t info)
        {
            return HuckForException(Tsapi.SetReconnectCall(activeCall, heldCall, resource, info),
                    String.Format("setReconnectCall {0}, {1} ,{2}, {3}", activeCall.callID, activeCall.deviceID, heldCall.callID, heldCall.deviceID));

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setRetrieveCall(ConnectionID_t heldCall)
        {

            return HuckForException(Tsapi.SetRetrieveCall(heldCall),
          String.Format("setRetrieveCall {0}, {1}", heldCall.callID, heldCall.deviceID));

       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setSendDTMFTone(ConnectionID_t call, string tone, int pauseDuartion)
        {
            return HuckForException(Tsapi.SetSendDTMFTone(call, tone, pauseDuartion),
            String.Format("setSendDTMFTone {0}, {1} ,{2}", call.callID, call.deviceID, tone));
       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setSetMsgWaitingInd(string deviceID, bool messages)
        {
            return HuckForException(Tsapi.SetSetMsgWaitingInd(deviceID, messages),
           String.Format("setSetMsgWaitingInd {0}, {1}", deviceID, messages));


        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<SnapshotCallEventReturn> setSnapshotCallReq(ConnectionID_t call)
        {
            return HuckForException(Tsapi.SetSnapshotCallReq(call),
                String.Format("setSnapshotCallReq {0}, {1}", call.callID, call.deviceID));

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<SnapshotDeviceEventReturn> setSnapshotDeviceReq(string deviceID)
        {

            return HuckForException(Tsapi.SetSnapshotDeviceReq(deviceID),
            String.Format("setSnapshotDeviceReq {0}", deviceID));
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<TransferCallEventReturn> setTransferCall(ConnectionID_t activeCall, ConnectionID_t heldCall)
        {

            return HuckForException(Tsapi.SetTransferCall(activeCall, heldCall),
         String.Format("setTransferCall {0}, {1}, {2}, {3}", activeCall.callID, activeCall.deviceID, heldCall.callID, heldCall.deviceID));

       }



       
        [OperationContract]
        
        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof (Exeption))]
        public Task<QueryAcdSplitEventReturn> getQueryAcdSplit(String device)
        {

            return HuckForException(Tsapi.GetQueryAcdSplit(device),String.Format("getQueryAcdSplit {0} ", device));
        }


        [OperationContract]

        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof(Exeption))]
        public Task<List<String>> getQueryAgentLogin(String device)
        {
            return HuckForException(Tsapi.GetQueryAgentLogin(device), String.Format("getQueryAgentLogin {0} ", device));
        }

        
      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public Task<uint> setMonitorCall(ConnectionID_t call)
      {
          return HuckForException(Tsapi.SetMonitorCall(call, default(CSTAMonitorFilter_t)),
              String.Format("setMonitorCall {0}, {1}", call.callID, call.deviceID)).ContinueWith(task =>
              {
                  var mon = task.Result;
                  mon.OnBackInService += mon_OnBackInService;
                  mon.OnCallCleared += mon_OnCallCleared;
                  mon.OnCallInformation += mon_OnCallInformation;
                  mon.OnConferenced += mon_OnConferenced;
                  mon.OnConnectionCleared += mon_OnConnectionCleared;
                  mon.OnDelivered += mon_OnDelivered;
                  mon.OnDiverted += mon_OnDiverted;
                  mon.OnDoNotDisturb += mon_OnDoNotDisturb;
                  mon.OnEstablished += mon_OnEstablished;
                  mon.OnFailed += mon_OnFailed;
                  mon.OnForwarding += mon_OnForwarding;
                  mon.OnHeld += mon_OnHeld;
                  mon.OnMessageWaiting += mon_OnMessageWaiting;
                  mon.OnMonitorEnded += mon_OnMonitorEnded;
                  mon.OnNetworkReached += mon_OnNetworkReached;
                  mon.OnOriginated += mon_OnOriginated;
                  mon.OnOutOfService += mon_OnOutOfService;
                  mon.OnPrivateStatus += mon_OnPrivateStatus;
                  mon.OnQueued += mon_OnQueued;
                  mon.OnRetrieved += mon_OnRetrieved;
                  mon.OnServiceInitiated += mon_OnServiceInitiated;
                  mon.OnTransferred += mon_OnTransferred;
                  SetCallBack(call.deviceID, call, TypeMonitor.Call);
                  return mon.GetTargertMonitrorId();
              });

          }

      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public Task<uint> setMonitorCallsViaDevice(string deviceID)
      {

          return HuckForException(Tsapi.SetMonitorCallsViaDevice(deviceID, default(CSTAMonitorFilter_t)),
              String.Format("setMonitorCallsViaDevice {0}", deviceID)).ContinueWith(task =>
              {
                  var mon = task.Result;
                  mon.OnBackInService += mon_OnBackInService;
                  mon.OnCallCleared += mon_OnCallCleared;
                  mon.OnCallInformation += mon_OnCallInformation;
                  mon.OnConferenced += mon_OnConferenced;
                  mon.OnConnectionCleared += mon_OnConnectionCleared;
                  mon.OnDelivered += mon_OnDelivered;
                  mon.OnDiverted += mon_OnDiverted;
                  mon.OnDoNotDisturb += mon_OnDoNotDisturb;
                  mon.OnEstablished += mon_OnEstablished;
                  mon.OnFailed += mon_OnFailed;
                  mon.OnForwarding += mon_OnForwarding;
                  mon.OnHeld += mon_OnHeld;
                  mon.OnMessageWaiting += mon_OnMessageWaiting;
                  mon.OnMonitorEnded += mon_OnMonitorEnded;
                  mon.OnNetworkReached += mon_OnNetworkReached;
                  mon.OnOriginated += mon_OnOriginated;
                  mon.OnOutOfService += mon_OnOutOfService;
                  mon.OnPrivateStatus += mon_OnPrivateStatus;
                  mon.OnQueued += mon_OnQueued;
                  mon.OnRetrieved += mon_OnRetrieved;
                  mon.OnServiceInitiated += mon_OnServiceInitiated;
                  mon.OnTransferred += mon_OnTransferred;
                  SetCallBack(deviceID, new ConnectionID_t { deviceID = deviceID }, TypeMonitor.DeviceVia);
                  return mon.GetTargertMonitrorId();
              });

        }


      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
       public Task<uint> setMonitorDevice(string deviceID )
      {
          return HuckForException(Tsapi.SetMonitorDevice(deviceID, default(CSTAMonitorFilter_t)),
              String.Format("setMonitorDevice {0}", deviceID)).ContinueWith(task =>
              {
                  var mon = task.Result;
                  mon.OnBackInService += mon_OnBackInService;
                  mon.OnCallCleared += mon_OnCallCleared;
                  mon.OnCallInformation += mon_OnCallInformation;
                  mon.OnConferenced += mon_OnConferenced;
                  mon.OnConnectionCleared += mon_OnConnectionCleared;
                  mon.OnDelivered += mon_OnDelivered;
                  mon.OnDiverted += mon_OnDiverted;
                  mon.OnDoNotDisturb += mon_OnDoNotDisturb;
                  mon.OnEstablished += mon_OnEstablished;
                  mon.OnFailed += mon_OnFailed;
                  mon.OnForwarding += mon_OnForwarding;
                  mon.OnHeld += mon_OnHeld;
                  mon.OnMessageWaiting += mon_OnMessageWaiting;
                  mon.OnMonitorEnded += mon_OnMonitorEnded;
                  mon.OnNetworkReached += mon_OnNetworkReached;
                  mon.OnOriginated += mon_OnOriginated;
                  mon.OnOutOfService += mon_OnOutOfService;
                  mon.OnPrivateStatus += mon_OnPrivateStatus;
                  mon.OnQueued += mon_OnQueued;
                  mon.OnRetrieved += mon_OnRetrieved;
                  mon.OnServiceInitiated += mon_OnServiceInitiated;
                  mon.OnTransferred += mon_OnTransferred;
                  SetCallBack(deviceID, new ConnectionID_t { deviceID = deviceID }, TypeMonitor.Device);
                  return mon.GetTargertMonitrorId();
              });

       }



        void mon_OnTransferred(object o, CSTAAttEventArgs<CSTATransferredEvent_t, ATTTransferredEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            Log.Debug(cstaAttEventArgs.CSTA);
            Log.Debug(cstaAttEventArgs.Att);
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnTransferred {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {


                mmm.Callback.OnTransferred(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnServiceInitiated(object o, CSTAAttEventArgs<CSTAServiceInitiatedEvent_t, ATTServiceInitiatedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnServiceInitiated {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnServiceInitiated(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnRetrieved(object o, CSTAEventArgs<CSTARetrievedEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnRetrieved {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnRetrieved(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnQueued(object o, CSTAAttEventArgs<CSTAQueuedEvent_t, ATTQueuedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnQueued {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnQueued(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnPrivateStatus(object o, CSTAEventArgs<CSTAPrivateStatusEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnPrivateStatus {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnPrivateStatus(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnOutOfService(object o, CSTAEventArgs<CSTAOutOfServiceEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnOutOfService {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnOutOfService(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnOriginated(object o, CSTAAttEventArgs<CSTAOriginatedEvent_t, ATTOriginatedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnOriginated {0}", con.deviceID);
            DuplexMonitor mmm;
            if (_monitors.TryGetValue(con.deviceID, out mmm))
               
            try
            {
                mmm.Callback.OnOriginated(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnNetworkReached(object o, CSTAAttEventArgs<CSTANetworkReachedEvent_t, ATTNetworkReachedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnNetworkReached {0}", con.deviceID);
            DuplexMonitor mmm;
            if (_monitors.TryGetValue(con.deviceID, out mmm))
               
            try
            {
                mmm.Callback.OnNetworkReached(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnMonitorEnded(object o, CSTAEventArgs<CSTAMonitorEndedEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnMonitorEnded {0}", con.deviceID);
            DuplexMonitor mmm;
            if (_monitors.TryGetValue(con.deviceID, out mmm))
            {
                try
                {
                    mmm.Callback.OnMonitorEnded(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex) { Log.Error(ex); }
           
                RemoveCallBack(con.deviceID);
            }
        }

        void mon_OnMessageWaiting(object o, CSTAEventArgs<CSTAMessageWaitingEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnMessageWaiting {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnMessageWaiting(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnHeld(object o, CSTAEventArgs<CSTAHeldEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnHeld {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnHeld(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnForwarding(object o, CSTAEventArgs<CSTAForwardingEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnForwarding {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnForwarding(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnFailed(object o, CSTAAttEventArgs<CSTAFailedEvent_t, ATTFailedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnFailed {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnFailed(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnEstablished(object o, CSTAAttEventArgs<CSTAEstablishedEvent_t, ATTEstablishedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnEstablished {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnEstablished(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDoNotDisturb(object o, CSTAEventArgs<CSTADoNotDisturbEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDoNotDisturb {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnDoNotDisturb(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDiverted(object o, CSTAAttEventArgs<CSTADivertedEvent_t, ATTDivertedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDiverted {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnDiverted(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDelivered(object o, CSTAAttEventArgs<CSTADeliveredEvent_t, ATTDeliveredEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDelivered {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnDelivered(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnConnectionCleared(object o, CSTAEventArgs<CSTAConnectionClearedEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnConnectionCleared {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnConnectionCleared(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnConferenced(object o, CSTAAttEventArgs<CSTAConferencedEvent_t, ATTConferencedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnConferenced {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnConferenced(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnCallInformation(object o, CSTAEventArgs<CSTACallInformationEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnCallInformation {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnCallInformation(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnCallCleared(object o, CSTAAttEventArgs<CSTACallClearedEvent_t, ATTCallClearedEvent_t> cstaAttEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnCallCleared {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnCallCleared(con.deviceID, cstaAttEventArgs.CSTA, cstaAttEventArgs.Att, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnBackInService(object o, CSTAEventArgs<CSTABackInServiceEvent_t> cstaEventArgs)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnBackInService {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm)) return;
            try
            {
                mmm.Callback.OnBackInService(con.deviceID, cstaEventArgs.CSTA, !sender.IsCallMonitor ? "" : con.callID.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex) { Log.Error(ex); }
        }
        
        protected Boolean SetCallBack(String key,ConnectionID_t target,TypeMonitor type)
        {
            if (OperationContext.Current == null) return false;
            var callback = OperationContext.Current.GetCallbackChannel<IDuplexCallback>();
            return _monitors.TryAdd(key, new DuplexMonitor { Callback = callback, Target = target, Type = type });
        }

        protected DuplexMonitor RemoveCallBack(String key)
        {
            DuplexMonitor cb;
            _monitors.TryRemove(key, out cb);
            return cb;
        }

      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public void setMonitorAgent(string agentID)
      {
             var con = new ConnectionID_t { deviceID = agentID };
              if (SetCallBack(agentID, con, TypeMonitor.Agent))
              {
                  var mon = Monitor.SetMonitorAgent(agentID);
                  mon.OnAftCall += mon_OnAftCall;
                  mon.OnLogin += mon_OnLogin;
                  mon.OnLogout += mon_OnLogout;
                  mon.OnNotReady += mon_OnNotReady;
                  mon.OnReady += mon_OnReady;
              }
    }


      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public void setMonitorAgentStop(string agentID)
      {
          Monitor.SetMonitorAgentStop(agentID);
          RemoveCallBack(agentID);
      }

      void mon_OnReady(object o, AgentStateEventArgs arg)
      {
          var mon = o as MonitorEventAgentCollection;
          if (mon == null) return;
          Log.DebugFormat("mon_OnReady {0}", mon.GetAgentName());
          DuplexMonitor mmm;
          if (_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            
              try
              {
                  mmm.Callback.OnReady(mon.GetAgentName(), arg.Mode);
              }
              catch (Exception ex) { Log.Error(ex); }
      }
      void mon_OnNotReady(object o, AgentStateEventArgs arg)
      {
          var mon = o as MonitorEventAgentCollection;
          if (mon == null) return;
          Log.DebugFormat("mon_OnNotReady {0}", mon.GetAgentName());
          DuplexMonitor mmm;
          if (_monitors.TryGetValue(mon.GetAgentName(), out mmm))
             
          try
          {
                mmm.Callback.OnNotReady(mon.GetAgentName(), arg.Mode);
          }
          catch (Exception ex) { Log.Error(ex); }
      }
      void mon_OnAftCall(object o, AgentStateEventArgs arg)
      {
          var mon = o as MonitorEventAgentCollection;
          if (mon == null) return;
          Log.DebugFormat("mon_OnAftCall {0}", mon.GetAgentName());
          DuplexMonitor mmm;
          if (_monitors.TryGetValue(mon.GetAgentName(), out mmm))
             
          try
          {
                mmm.Callback.OnAftCall(mon.GetAgentName(), arg.Mode);
          }
          catch (Exception ex) { Log.Error(ex); }
      }
      void mon_OnLogout(object o, AgentStateEventArgs args)
      {
          var mon = o as MonitorEventAgentCollection;
          if (mon == null) return;
          Log.DebugFormat("mon_OnLogout {0}", mon.GetAgentName());
          DuplexMonitor mmm;
          if (_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            
          try
          {
                mmm.Callback.OnLogout(mon.GetAgentName());
          }
          catch (Exception ex) { Log.Error(ex); }
        
      }
      void mon_OnLogin(object o, AgentStateEventArgs args)
      {
          var mon = o as MonitorEventAgentCollection;
          if (mon == null) return;
          Log.DebugFormat("mon_OnLogin {0}", mon.GetAgentName());
          DuplexMonitor mmm;
          if(_monitors.TryGetValue( mon.GetAgentName(), out mmm))
              
          try
          {
            mmm.Callback.OnLogin(mon.GetAgentName());
          }
          catch (Exception ex) { Log.Error(ex); }
      }

      

     

    
        
      public void Dispose()
      {
          Log.Info(String.Format("TsapiServer instance end."));
          InWork = false;
          if(_monitors != null)_monitors.Clear();
          if (_mainConnect != null)
          {
              if (_mainConnect.StatusConnection == StatusConection.Open)
              {
                  _mainConnect.AbortStream();
                  _mainConnect.Dispose();
              }
          }
          if (_subConnect != null)
          {
              if (_subConnect.StatusConnection == StatusConection.Open)
              {
                  _subConnect.AbortStream();
                  _subConnect.Dispose();
              }
          }
          _monitors = null;
          _subConnect = null;
          _mainConnect = null;
      }
        // ReSharper restore InconsistentNaming

    }
}
