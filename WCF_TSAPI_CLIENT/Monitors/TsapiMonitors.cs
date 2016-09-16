using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using log4net;
using WCF_TSAPI_CLIENT.TsapiServer;

namespace WCF_TSAPI_CLIENT.Monitors
{
    class Monitor
    {
        public  TsapiMonitor Mon { get; set; }
        public Func<Task<TsapiMonitor>> Start { get; set; }
        public Func<Task<setMonitorStopResponse>> Stop { get; set; }
    }

    [CallbackBehavior(ConcurrencyMode =  ConcurrencyMode.Multiple,IncludeExceptionDetailInFaults = true,UseSynchronizationContext = true,ValidateMustUnderstand = true)]
    class TsapiMonitors : ConcurrentDictionary<uint, Monitor>, TsapiServerCallback
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TsapiMonitors));


        public delegate void SerevrFail();

        public event SerevrFail OnServerFail;
        public void Remove(TsapiMonitor mon)
        {
            Monitor mon2;
            TryRemove(mon.IdMonitor, out mon2);
            mon2.Stop();
        }

        private TsapiMonitor Get(uint monId)
        {
            Monitor mon;
            if (TryGetValue(monId, out mon))
            {
                return mon.Mon;
            }
            Log.Error($"Monitor {monId} not found");
            //Environment.Exit(100);
            return null;
        }


        public void Reset()
        {
            var mons = ToArray();
            
            foreach (var mon in mons)
            {
                ResetSetup(mon.Key, mon.Value);
            }
        }

        private void ResetSetup(uint key, Monitor mon)
        {
            Log.Debug("ResetSetup");
            Monitor m;
            if (TryRemove(key, out m))
            {

            }
            mon.Start?.Invoke().ContinueWith(task =>
            {
                task.Result.CopyEvent(mon);
            });
        }
        

        public TsapiMonitor Add(uint monId, Func<Task<TsapiMonitor>> start, Func<Task<setMonitorStopResponse>> stop)
        {
            var mon = new Monitor {Start = start, Stop  = stop, Mon = new TsapiMonitor(monId) };
            for (var x = 0; x < 3; x++)
            {
                if (TryAdd(monId, mon))
                {
                    return mon.Mon;
                }
                Monitor t;
                TryRemove(monId, out t);
            }
            return null;
        }

 

        public void OnLogoutDevice(CSTALoggedOffEvent_t agent, uint monId)
        {
            Get(monId).LogoutDevice(agent);
        }

        public void OnCallCleared(OnCallCleared param)
        {
            Get(param.monId).CallClearedCall(param.csta, param.att);
        }


        public void OnConferenced(OnConferenced param)
        {
            Get(param.monId).ConferencedCall(param.csta, param.att);
        }

        public void OnConnectionCleared(OnConnectionCleared param)
        {
            Get(param.monId).ConnectionClearedCall(param.csta);
        }

        public void OnDelivered(OnDelivered param)
        {
            Get(param.monId).DeliveredCall(param.csta, param.att);
        }

        public void OnDiverted(OnDiverted param)
        {
            Get(param.monId).DivertedCall(param.csta, param.att);
        }


        public void OnEstablished(OnEstablished param)
        {
            Get(param.monId).EstablishedCall(param.csta, param.att);
        }

        public void OnFailed(OnFailed param)
        {
            Get(param.monId).FailedCall(param.csta, param.att);
        }

        public void OnHeld(OnHeld param)
        {
            Get(param.monId).HeldCall(param.csta);
        }

        public void OnNetworkReached(OnNetworkReached param)
        {
            Get(param.monId).NetworkReachedCall(param.csta, param.att);
        }

        public void OnOriginated(OnOriginated param)
        {
            Get(param.monId).OriginatedCall(param.csta, param.att);
        }

        public void OnQueued(OnQueued param)
        {
            Get(param.monId).QueuedCall(param.csta, param.att);
        }

        public void OnRetrieved(OnRetrieved param)
        {
            Get(param.monId).RetrievedCall(param.csta);
        }

        public void OnServiceInitiated(OnServiceInitiated param)
        {
            Get(param.monId).ServiceInitiatedCall(param.csta, param.att);
        }

        public void OnTransferred(OnTransferred param)
        {
            Get(param.monId).TransferredCall(param.csta, param.att);
        }

        public void OnCallInformation(OnCallInformation param)
        {
            Get(param.monId).CallInformationCall(param.csta);
        }

        public void OnDoNotDisturb(OnDoNotDisturb param)
        {
            Get(param.monId).DoNotDisturbCall(param.csta);
        }

        public void OnForwarding(OnForwarding param)
        {
            Get(param.monId).ForwardingCall(param.csta);
        }

        public void OnMessageWaiting(OnMessageWaiting param)
        {
            Get(param.monId).MessageWaitingCall(param.csta);
        }

        public void OnBackInService(OnBackInService param)
        {
            Get(param.monId).BackInServiceCall(param.csta);
        }

        public void OnOutOfService(OnOutOfService param)
        {
            Get(param.monId).OutOfServiceCall(param.csta);
        }

        public void OnPrivateStatus(OnPrivateStatus param)
        {
            Get(param.monId).PrivateStatusCall(param.csta);
        }

        public void OnMonitorEnded(OnMonitorEnded param)
        {
            Monitor mon;
            if (!TryRemove(param.monId, out mon)) return;
            mon?.Mon.MonitorEnd(param.csta, param);
            ResetSetup(param.monId, mon);
        }

        public void ServerFail()
        {
            OnServerFail?.Invoke();
        }

        public void OnReady(OnReady param)
        {
            Get(param.monId).Ready(param);
        }

        public void OnNotReady(OnNotReady param)
        {
            Get(param.monId).NotReady(param);
        }

        public void OnAftCall(OnAftCall param)
        {
            Get(param.monId).AftCall(param);
        }

        public void OnLogin(OnLogin param)
        {
            Get(param.monId).Login(param);
        }

        public void OnLoginDevice(CSTALoggedOnEvent_t arg, uint monId)
        {
            Get(monId).LoginDevice(arg);
        }
        
        public void OnLogout(OnLogout param)
        {
            Get(param.monId).Logout(param);
        }


     
    }
}
