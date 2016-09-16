using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using log4net;
using TSAPILIB2;
using WCF_TSAPI_SERVER.Properties;

namespace WCF_TSAPI_SERVER
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext=true, IncludeExceptionDetailInFaults=true)]
    [ServiceContract(CallbackContract = typeof(IDuplexCallback), SessionMode=SessionMode.Required)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession, 
        IncludeExceptionDetailInFaults = true, UseSynchronizationContext = true)]
    class TsapiServer:IDisposable
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(TsapiServer)); 
        private ConcurrentDictionary<string, DuplexMonitor> _monitors = new ConcurrentDictionary<string, DuplexMonitor>();
        Tsapi _mainConnect;
        
      
        
       // protected bool InWork = true;
       /* protected Tsapi Tsapi
        {
            get
            {
               
                    if (_mainConnect == null)
                    {
                        lock (_mainConnectLocked)
                        {
                            if (_mainConnect == null)
                            {
                                _mainConnect = new Tsapi(Settings.Default.server, Settings.Default.login,
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
                
                Log.DebugFormat("TSAPI main_connect  Current status {0}", _mainConnect.StatusConnection);
                throw  new Exception("TSAPI main connect no openned!");
            }
        }*/

        private Tsapi GetConnection()
        {

            if (_mainConnect == null)
            {
                lock (_mainConnectLocked)
                {
                    if (_mainConnect == null)
                    {
                        _mainConnect = new Tsapi(Settings.Default.server, Settings.Default.login,
                            Settings.Default.password, GetType().Name + "_main", Settings.Default.api,
                            Settings.Default.version);
                        _mainConnect.OnConnnectedEvent += main_connect_OnConnnected;
                        _mainConnect.OnClosedEvent += main_connect_OnClosed;
                        _mainConnect.OpenStream();
                    }
                    
                }

            }
            return _mainConnect;
        }
        readonly object _mainConnectLocked = new object();

       
        private Task<T> HuckForException<T>(Func<Tsapi,Task<T>> t, string message = "")
        {
            var connect = GetConnection();
            return t(connect).ContinueWith(c =>
            {
                try
                {
                    return c.Result;
                }
                catch (AggregateException ex)
                {
                    if(connect.StatusConnection != StatusConection.Open)
                        throw new FaultException<CSTAFail>(new CSTAFail(CSTAUniversalFailure_t.serviceTerminationRejection), CSTAUniversalFailure_t.serviceTerminationRejection.ToString());
                    ex.Handle(k => AnalizError(k.InnerException.GetBaseException(), message));
                }
                return default(T);
            });
        }
        private bool AnalizError(Exception innerException, string message)
        {

            var csta = innerException as CstaExeption;
            var asc = innerException as Exeption;
            Log.Error(message, innerException);
            if (csta != null) throw new FaultException<CSTAFail>(new CSTAFail(csta.Code), csta.Code.ToString());
            if (asc != null) throw new FaultException<ACSFail>(new ACSFail(asc.Code), asc.Code.ToString());
            throw new FaultException<SYSFail>(new SYSFail(innerException), innerException.ToString());
        }
        ~TsapiServer()
        {
            Dispose();
        }
        public TsapiServer()
        {
            Log.Info("TsapiServer instance start.");

        }

        protected void main_connect_OnClosed(object sender, EventArgs arg)
        {

            /*_monitors.ToList().ForEach(pair =>
            {
                pair.Value.Callback?.ServerFail();
            });*/
            var tsapi = sender as Tsapi;
            if (tsapi != null)
            {

                Log.Info($"TSAPI Connect {tsapi.LinkName} close.");
               // if (!InWork) return;
              //  tsapi.OpenStream();
            }

          //  OperationContext.Current?.GetCallbackChannel<IDuplexCallback>()?.ServerFail();
            OperationContext.Current?.Channel.Close();

            /* foreach (var mon in _monitors.Values)
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
             }*/
        }

        protected void main_connect_OnConnnected(object sender, EventArgs  arg)
        {
            
            Log.Info($"TSAPI Connect {((Tsapi) sender).LinkName} open.");
        }

        // ReSharper disable InconsistentNaming        
        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryDeviceInfoReturn> getQueryDeviceInfo(string device)
        {
            
            return HuckForException(t=>t.GetQueryDeviceInfo(device), $"getQueryDeviceInfo {device}");
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryLastNumberEventReturn> getQueryLastNumber(string deviceID)
        {
            return HuckForException(t=>t.GetQueryLastNumber(deviceID), $"getQueryLastNumber {deviceID}");
         }


        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))]
        [FaultContract(typeof(SYSFail))]
        public Task<QueryAgentStateEventReturn> getQueryAgentState(string deviceID)
        {
            return HuckForException(t=>t.GetQueryAgentState(deviceID), $"getQueryAgentState {deviceID}");
        }



        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryMsgWaitingEventReturn> getQueryMsgWaitingInd(string deviceID)
        {
            return HuckForException(t=>t.GetQueryMsgWaitingInd(deviceID), $"getQueryMsgWaitingInd {deviceID}");
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryStationStatusEventReturn> getQueryStationStatus(string deviceID)
        {
            return HuckForException(t=>t.GetQueryStationStatus(deviceID), $"getQueryStationStatus {deviceID}");
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryUcidEventReturn> getQueryUCID(ConnectionID_t call)
        {
            return HuckForException(t=>t.GetQueryUcid(call),
                $"getQueryUCID {call.callID} {call.deviceID} {call.devIDType}");
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAgentState(string deviceID, string agentID, string agentGroup, string password, AgentMode_t mode, ATTWorkMode_t wmode, int reasonCode)
        {
             return HuckForException(t=>t.SetAgentState(deviceID, agentID, agentGroup, password, mode, wmode, reasonCode),
                 $"setAgentState {deviceID} {agentID} {agentGroup} {password}");
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAlternateCall(ConnectionID_t activeCall, ConnectionID_t otherCall)
        {

            return HuckForException(t=>t.SetAlternateCall(activeCall, otherCall),
                $"setAlternateCall {activeCall.callID} {activeCall.deviceID} {otherCall.callID} {otherCall.deviceID}");
       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setAnswerCall(ConnectionID_t allertingCall)
        {
            return HuckForException(t=>t.SetAnswerCall(allertingCall),
                $"setAnswerCall {allertingCall.callID} {allertingCall.deviceID}");
         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setCallCompletion(ConnectionID_t call, Feature_t feature)
        {

            return HuckForException(t=>t.SetCallCompletion(call, feature),
                $"setCallCompletion {call.callID} {call.deviceID}");
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ChangeMonitorFilterEventReturn> setChangeMonitorFilter(uint monitorCrossId, CSTAMonitorFilter_t filter)
        {
            return HuckForException(t=>t.SetChangeMonitorFilter(monitorCrossId, filter),
                $"setChangeMonitorFilter {monitorCrossId}");

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setClearCall(ConnectionID_t call)
        {
            return HuckForException(t=>t.SetClearCall(call),
                $"setClearCall {call.callID} {call.deviceID}");

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setClearConnection(ConnectionID_t call, ATTDropResource_t resourse, string info)
        {
            return HuckForException(t=>t.SetClearConnection(call, resourse, info),
                $"setClearConnection {call.callID} {call.deviceID}");

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ConferenceCallEventReturn> setConferenceCall(ConnectionID_t activeCall, ConnectionID_t otherCall)
        {
            return HuckForException(t=>t.SetConferenceCall(activeCall, otherCall),
                $"setConferenceCall {activeCall.callID} {activeCall.deviceID} {otherCall.callID} {otherCall.deviceID}");
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<ConsultationCallEventReturn> setConsultationCall(ConnectionID_t activeCall, string calledDevice, string destRoute, bool priorityCalling, string info)
        {
            
            return HuckForException(t=>t.SetConsultationCall(activeCall, calledDevice, destRoute, priorityCalling, info),
                $"setConsultationCall {activeCall.callID} {activeCall.deviceID} {calledDevice}");

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setDeflectCall(ConnectionID_t deflectCall, string calledDevice)
        {
            return HuckForException(t=>t.SetDeflectCall(deflectCall, calledDevice),
                $"setDeflectCall {deflectCall.callID} {deflectCall.deviceID} {calledDevice}");


         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setGroupPickupCall(ConnectionID_t deflectCall, string pickupDevice)
        {

            return HuckForException(t=>t.SetGroupPickupCall(deflectCall, pickupDevice),
                $"setGroupPickupCall {deflectCall.callID} {deflectCall.deviceID} {pickupDevice}");

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setHoldCall(ConnectionID_t activeCall)
        {
            return HuckForException(t=>t.SetHoldCall(activeCall),
                $"setHoldCall  {activeCall.callID}, {activeCall.deviceID}");

         }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<MakeCallEventReturn> setMakeCall(string callingDevice, string calledDevice, string destroute, bool priorityCall, string info)
        {
            return HuckForException(t=>t.SetMakeCall(callingDevice, calledDevice, destroute, priorityCall, info),
                $"setMakeCall {callingDevice}, {calledDevice}");
        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<MakePredictiveCallEventReturn> setMakePredictiveCall(string callingDevice, string calledDevice, AllocationState_t allocationState, string destRoute, bool priorityCalling, short maxRing, ATTAnswerTreat_t answerTreat, string info)
        {
            return HuckForException(t=>t.SetMakePredictiveCall(callingDevice, calledDevice, allocationState, destRoute, priorityCalling, maxRing, answerTreat, info),
                $"setMakePredictiveCall  {callingDevice}, {calledDevice}");
        }
      

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setMonitorStop(uint monitorCrossId)
        {
            return HuckForException(t=>t.SetMonitorStop(monitorCrossId),
                $"setMonitorStop {monitorCrossId}");

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setPickupCall(ConnectionID_t deflectCall, string calledDevice)
        {
            return HuckForException(t=>t.SetPickupCall(deflectCall, calledDevice),
                $"getQueryDeviceInfo {deflectCall.callID} {deflectCall.deviceID} {calledDevice}");

          }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<QueryCallMonitorEventReturn> setQueryCallMonitor(string deviceID)
        {

            return HuckForException(t=>t.SetQueryCallMonitor(deviceID),
                $"setQueryCallMonitor {deviceID}");


       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setReconnectCall(ConnectionID_t activeCall, ConnectionID_t heldCall, ATTDropResource_t resource, string info)
        {
            return HuckForException(t=>t.SetReconnectCall(activeCall, heldCall, resource, info),
                $"setReconnectCall {activeCall.callID}, {activeCall.deviceID} ,{heldCall.callID}, {heldCall.deviceID}");

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setRetrieveCall(ConnectionID_t heldCall)
        {

            return HuckForException(t=>t.SetRetrieveCall(heldCall),
                $"setRetrieveCall {heldCall.callID}, {heldCall.deviceID}");

       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setSendDTMFTone(ConnectionID_t call, string tone, int pauseDuartion)
        {
            return HuckForException(t=>t.SetSendDtmfTone(call, tone, pauseDuartion),
                $"setSendDTMFTone {call.callID}, {call.deviceID} ,{tone}");
       }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<NullTsapiReturn> setSetMsgWaitingInd(string deviceID, bool messages)
        {
            return HuckForException(t=>t.SetSetMsgWaitingInd(deviceID, messages),
                $"setSetMsgWaitingInd {deviceID}, {messages}");


        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<SnapshotCallEventReturn> setSnapshotCallReq(ConnectionID_t call)
        {
            return HuckForException(t=>t.SetSnapshotCallReq(call),
                $"setSnapshotCallReq {call.callID}, {call.deviceID}");

        }

        [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public Task<SnapshotDeviceEventReturn> setSnapshotDeviceReq(string deviceID)
        {
            
            var ret = HuckForException(t=>t.SetSnapshotDeviceReq(deviceID),
                $"setSnapshotDeviceReq {deviceID}");
            return ret;
        }

        [OperationContract]
        [FaultContract(typeof (CSTAFail))]
        [FaultContract(typeof (ACSFail))]
        [FaultContract(typeof (SYSFail))]
        [FaultContract(typeof (Exeption))]

        public Task<TransferCallEventReturn> setTransferCall(ConnectionID_t activeCall, ConnectionID_t heldCall)
        {

            return HuckForException(t => t.SetTransferCall(activeCall, heldCall),
                $"setTransferCall {activeCall.callID}, {activeCall.deviceID}, {heldCall.callID}, {heldCall.deviceID}");
        }




        [OperationContract]
        
        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof (Exeption))]
        public Task<QueryAcdSplitEventReturn> getQueryAcdSplit(string device)
        {

            return HuckForException(t=>t.GetQueryAcdSplit(device), $"getQueryAcdSplit {device} ");
        }


        [OperationContract]

        [FaultContract(typeof(CSTAFail))]
        [FaultContract(typeof(Exeption))]
        public Task<List<string>> getQueryAgentLogin(string device)
        {
            return HuckForException(t=>t.GetQueryAgentLogin(device), $"getQueryAgentLogin {device} ");
        }

        
      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public Task<uint> setMonitorCall(ConnectionID_t call)
      {
          SetCallBack(call.deviceID, call, TypeMonitor.Call);
          return HuckForException(t=>t.SetMonitorCall(call, default(CSTAMonitorFilter_t)),
              $"setMonitorCall {call.callID}, {call.deviceID}").ContinueWith(task =>
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
                    return mon.GetTargertMonitrorId();
                });

          /*return HuckForException(t=>t.SetMonitorCall(call, default(CSTAMonitorFilter_t)),
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
              });*/

      }

      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
      public Task<uint> setMonitorCallsViaDevice(string deviceID)
      {
          SetCallBack(deviceID, new ConnectionID_t { deviceID = deviceID }, TypeMonitor.DeviceVia);
          return HuckForException(t=>t.SetMonitorCallsViaDevice(deviceID, default(CSTAMonitorFilter_t)),
              $"setMonitorCallsViaDevice {deviceID}").ContinueWith(task =>
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
                  return mon.GetTargertMonitrorId();
              });

        }


      [OperationContract]
      [FaultContract(typeof(CSTAFail))]
     [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
       public Task<uint> setMonitorDevice(string deviceID )
      {
          SetCallBack(deviceID, new ConnectionID_t { deviceID = deviceID }, TypeMonitor.Device);
          return HuckForException(t=>t.SetMonitorDevice(deviceID, default(CSTAMonitorFilter_t)),
              $"setMonitorDevice {deviceID}").ContinueWith(task =>
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
                  mon.OnLogOn += MonOnOnLogOn;
                  mon.OnLogOff += MonOnOnLogOff;
                  return mon.GetTargertMonitrorId();
              });

       }

        private void MonOnOnLogOn(object sender, CstaEventArgs<CSTALoggedOnEvent_t> cstaEventArgs, uint monId)
        {

            Log.DebugFormat("MonOnOnLogOn {0}", cstaEventArgs.Csta.agentID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(cstaEventArgs.Csta.agentGroup, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", cstaEventArgs.Csta.agentID);
                return;
            }
            try
            {
                
                 mmm.Callback?.OnLoginDevice(cstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }


        private void MonOnOnLogOff(object sender, CstaEventArgs<CSTALoggedOffEvent_t> cstaEventArgs, uint monId)
        {

            Log.DebugFormat("MonOnOnLogOff {0}", cstaEventArgs.Csta.agentID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(cstaEventArgs.Csta.agentGroup, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", cstaEventArgs.Csta.agentID);
                return;
            }
            try
            {
                 mmm.Callback?.OnLogoutDevice(cstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }


        void mon_OnTransferred(object o, CstaAttEventArgs<CSTATransferredEvent_t, ATTTransferredEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            Log.Debug(CstaAttEventArgs.Csta);
            Log.Debug(CstaAttEventArgs.Att);
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnTransferred {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {

                 mmm.Callback?.OnTransferred(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnServiceInitiated(object o, CstaAttEventArgs<CSTAServiceInitiatedEvent_t, ATTServiceInitiatedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnServiceInitiated {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnServiceInitiated(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnRetrieved(object o, CstaEventArgs<CSTARetrievedEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnRetrieved {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnRetrieved(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnQueued(object o, CstaAttEventArgs<CSTAQueuedEvent_t, ATTQueuedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnQueued {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnQueued(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnPrivateStatus(object o, CstaEventArgs<CSTAPrivateStatusEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnPrivateStatus {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnPrivateStatus(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnOutOfService(object o, CstaEventArgs<CSTAOutOfServiceEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnOutOfService {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnOutOfService(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnOriginated(object o, CstaAttEventArgs<CSTAOriginatedEvent_t, ATTOriginatedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnOriginated {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnOriginated(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnNetworkReached(object o, CstaAttEventArgs<CSTANetworkReachedEvent_t, ATTNetworkReachedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnNetworkReached {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
               
            try
            {
                 mmm.Callback?.OnNetworkReached(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnMonitorEnded(object o, CstaEventArgs<CSTAMonitorEndedEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            
            if (sender == null) return;
            ConnectionID_t con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnMonitorEnded {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            {
                try
                {
                     mmm.Callback?.OnMonitorEnded(con.deviceID, CstaEventArgs.Csta, monId);
                }
                catch (Exception ex) { Log.Error(ex); }
                if (!sender.IsCallMonitor)
                {
                    RemoveCallBack(con.deviceID);
                }
            }
        }

        void mon_OnMessageWaiting(object o, CstaEventArgs<CSTAMessageWaitingEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnMessageWaiting {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnMessageWaiting(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnHeld(object o, CstaEventArgs<CSTAHeldEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnHeld {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnHeld(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnForwarding(object o, CstaEventArgs<CSTAForwardingEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnForwarding {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnForwarding(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnFailed(object o, CstaAttEventArgs<CSTAFailedEvent_t, ATTFailedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnFailed {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnFailed(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnEstablished(object o, CstaAttEventArgs<CSTAEstablishedEvent_t, ATTEstablishedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnEstablished {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnEstablished(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDoNotDisturb(object o, CstaEventArgs<CSTADoNotDisturbEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDoNotDisturb {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnDoNotDisturb(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDiverted(object o, CstaAttEventArgs<CSTADivertedEvent_t, ATTDivertedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDiverted {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnDiverted(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnDelivered(object o, CstaAttEventArgs<CSTADeliveredEvent_t, ATTDeliveredEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnDelivered {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnDelivered(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnConnectionCleared(object o, CstaEventArgs<CSTAConnectionClearedEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnConnectionCleared {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                
                 mmm.Callback?.OnConnectionCleared(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnConferenced(object o, CstaAttEventArgs<CSTAConferencedEvent_t, ATTConferencedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnConferenced {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnConferenced(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnCallInformation(object o, CstaEventArgs<CSTACallInformationEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnCallInformation {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnCallInformation(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnCallCleared(object o, CstaAttEventArgs<CSTACallClearedEvent_t, ATTCallClearedEvent_t> CstaAttEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnCallCleared {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {
                 mmm.Callback?.OnCallCleared(con.deviceID, CstaAttEventArgs.Csta, CstaAttEventArgs.Att, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

        void mon_OnBackInService(object o, CstaEventArgs<CSTABackInServiceEvent_t> CstaEventArgs, uint monId)
        {
            var sender = o as MonitorEventCollection;
            if (sender == null) return;
            var con = sender.GetTargertMonitror();
            Log.DebugFormat("mon_OnBackInService {0}", con.deviceID);
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(con.deviceID, out mmm))
            {
                Log.ErrorFormat("Device {0} not found in _monitors", con.deviceID);
                return;
            }
            try
            {

                mmm.Callback?.OnBackInService(con.deviceID, CstaEventArgs.Csta, monId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }
        
        protected Boolean SetCallBack(String key,ConnectionID_t target,TypeMonitor type)
        {
            if (OperationContext.Current == null || key == null) return false;
            var callback = OperationContext.Current.GetCallbackChannel<IDuplexCallback>();
            
            return _monitors.TryAdd(key, new DuplexMonitor { Callback = callback, Target = target, Type = type });
        }

        protected DuplexMonitor RemoveCallBack(String key)
        {
            DuplexMonitor cb;
            _monitors.TryRemove(key, out cb);
            return cb;
        }

        /*  [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
          public Task<uint> setMonitorAgent(string agentID)
        {
             var con = new ConnectionID_t { deviceID = agentID };
                if (SetCallBack(agentID, con, TypeMonitor.Agent))
                {
                    var mon = Tsapi.SetMonitorAgent(agentID);
                    mon.OnAftCall += mon_OnAftCall;
                    mon.OnLogin += mon_OnLogin;
                    mon.OnLogout += mon_OnLogout;
                    mon.OnNotReady += mon_OnNotReady;
                    mon.OnReady += mon_OnReady;
                    return Task.Run(() => mon.MonitorId);
                }
            return null;
        }


          [OperationContract]
        [FaultContract(typeof(CSTAFail))]
       [FaultContract(typeof(ACSFail))][FaultContract(typeof(SYSFail))]
        public void setMonitorAgentStop(string agentID)
        {
            Tsapi.SetMonitorAgentStop(agentID);
            RemoveCallBack(agentID);
        }

        void mon_OnReady(object o, AgentStateEventArgs arg, uint monitorId)
        {
            var mon = o as MonitorEventAgentCollection;
            if (mon == null) return;
            Log.DebugFormat("mon_OnReady {0}", mon.GetAgentName());
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            {
                Log.ErrorFormat("Agents {0} not found in _monitors", mon.GetAgentName());
                return;
            }

                try
                {
                     mmm.Callback?.OnReady(mon.GetAgentName(), arg.Mode, monitorId);
                }
                catch (Exception ex) { Log.Error(ex); }
        }
        void mon_OnNotReady(object o, AgentStateEventArgs arg, uint monitorId)
        {
            var mon = o as MonitorEventAgentCollection;
            if (mon == null) return;
            Log.DebugFormat("mon_OnNotReady {0}", mon.GetAgentName());
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            {
                Log.ErrorFormat("Agents {0} not found in _monitors", mon.GetAgentName());
                return;
            }
            try
            {
                 mmm.Callback?.OnNotReady(mon.GetAgentName(), arg.Mode, monitorId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }
        void mon_OnAftCall(object o, AgentStateEventArgs arg, uint monitorId)
        {
            var mon = o as MonitorEventAgentCollection;
            if (mon == null) return;
            Log.DebugFormat("mon_OnAftCall {0}", mon.GetAgentName());
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            {
                Log.ErrorFormat("Agents {0} not found in _monitors", mon.GetAgentName());
                return;
            }
            try
            {
                 mmm.Callback?.OnAftCall(mon.GetAgentName(), arg.Mode, monitorId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }
        void mon_OnLogout(object o, AgentStateEventArgs args, uint monitorId)
        {
            var mon = o as MonitorEventAgentCollection;
            if (mon == null) return;
            Log.DebugFormat("mon_OnLogout {0}", mon.GetAgentName());
            DuplexMonitor mmm;
            if (!_monitors.TryGetValue(mon.GetAgentName(), out mmm))
            {
                Log.ErrorFormat("Agents {0} not found in _monitors", mon.GetAgentName());
                return;
            }
            try
            {
                 mmm.Callback?.OnLogout(mon.GetAgentName(), monitorId);
            }
            catch (Exception ex) { Log.Error(ex); }

        }
        void mon_OnLogin(object o, AgentStateEventArgs args, uint monitorId)
        {
            var mon = o as MonitorEventAgentCollection;
            if (mon == null) return;
            Log.DebugFormat("mon_OnLogin {0}", mon.GetAgentName());
            DuplexMonitor mmm;
            if(!_monitors.TryGetValue( mon.GetAgentName(), out mmm))
            {
                Log.ErrorFormat("Agents {0} not found in _monitors", mon.GetAgentName());
                return;
            }  
            try
            {
                 mmm.Callback?.OnLogin(mon.GetAgentName(), monitorId);
            }
            catch (Exception ex) { Log.Error(ex); }
        }

          */
        public void Dispose()
      {
          Log.Info("TsapiServer instance end.");
     //     InWork = false;
          _monitors?.Clear();
          if (_mainConnect?.StatusConnection == StatusConection.Open)
          {
              _mainConnect.AbortStream();
              _mainConnect.Dispose();
          }

          _monitors = null;
          _mainConnect = null;
      }
        // ReSharper restore InconsistentNaming

    }
}
