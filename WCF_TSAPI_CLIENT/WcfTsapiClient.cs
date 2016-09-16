using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using WCF_TSAPI_CLIENT.Monitors;
using WCF_TSAPI_CLIENT.TsapiServer;
using Exception = System.Exception;

namespace WCF_TSAPI_CLIENT
{
    public static  class  WcfTsapiClient 
    {
        
        private static long _requests;
        private const long MaxRequest = (long) (long.MaxValue*0.7);
        private static readonly ConcurrentDictionary<int, WcfProxy> WcfProxies = new ConcurrentDictionary<int, WcfProxy>();
        private static readonly ILog Log = LogManager.GetLogger(typeof (WcfTsapiClient));

        static WcfTsapiClient()
        {
            XmlConfigurator.Configure();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            AddProxy();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Close();

        }


        public static void Init(int countInstance= 1)
        {
           
            for(var x = 0;x< countInstance-1;x++)
            AddProxy();
        }

        private static void AddProxy()
        {
            var x = WcfProxies.Count;
            WcfProxies.TryAdd(x, new WcfProxy(x));
        }
        
        private static int GetNextId()
        {
            var c = Interlocked.Increment(ref _requests);
            if (c >= MaxRequest)
                c = Interlocked.Exchange(ref _requests, 0);
            return (int) c%WcfProxies.Count;
        }

        private static WcfProxy GetInstance()
        {

            var id = GetNextId();
            if (WcfProxies.ContainsKey(id))
            {
                for (var x = 0; x < 3; x++)
                {
                    WcfProxy inst;
                    if (WcfProxies.TryGetValue(id, out inst))
                    {
                        return inst;
                    }
                }
            }

            Log.Debug($"Instance {id} not found");
            return null;
        }

    
        public static async Task<SnapshotDeviceEventReturn> SnapshotDeviceReqAsync(int devcieId)
        {
            try
            {
                var ret = await GetInstance().Exec(s => s.setSnapshotDeviceReqAsync(devcieId.ToString()),
                            $"SnapshotDeviceReqAsync({devcieId})");
                return ret.setSnapshotDeviceReqResult;
            }
            catch (Exception)
            {
                throw new CstaException(CSTAUniversalFailure_t.invalidCstaDeviceIdentifier);
            }
        }


        public static async Task<TsapiMonitor> MonitroDeviceAsync(int deviceId, bool createNewInstanseMon = true)
        {
            try
            {
                var instance = GetInstance();
                var ret = await instance.Exec(s => s.setMonitorDeviceAsync(deviceId.ToString()),
                    $"SetMonitroDevice({deviceId})");
                Log.Debug($"SetMonitroDevice({ deviceId}) = {ret.setMonitorDeviceResult}");
                return instance.Monitors.Add(
                    ret.setMonitorDeviceResult,
                    () => MonitroDeviceAsync(deviceId,false), 
                    () => instance.Exec(t=>t.setMonitorStopAsync(ret.setMonitorDeviceResult), $"setMonitorStopAsync({deviceId})"));
            }
            catch (Exception)
            {
                return null;
            }

        }
        public static async Task<QueryDeviceInfoReturn> QueryDeviceInfoAsync(int agent)
        {
            var ret =  await GetInstance().Exec(s => s.getQueryDeviceInfoAsync(agent.ToString()), $"GetDeviceId({agent})");
            return ret.getQueryDeviceInfoResult;

        }
        public static async Task<string[]> QueryListDevice(int i)
        {
            var ret =  await GetInstance().Exec(s => s.getQueryAgentLoginAsync(i.ToString()), $"QueryListDevice({i})");
            return ret.getQueryAgentLoginResult;
        }
        public static async Task<string> GetUcid(ConnectionID_t dd)
        {
            try
            {
                var ucid =  await GetInstance().Exec(s => s.getQueryUCIDAsync(dd), $"GetUcid({dd.deviceID}:{dd.callID})");
                return ucid.Att.ucid;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public static void MonitroDeviceStopAsync(TsapiMonitor mon)
        {
            var inst = GetInstance();
            GetInstance().Monitors.Remove(mon);
            
        }

        public static void Close()
        {
            WcfProxies.ToList().ForEach(t=>t.Value.CloseConnect());
        }

        public static  async Task<QueryAgentStateEventReturn> QueryAgentState(string agent)
        {
            var ret = await GetInstance().Exec(s => s.getQueryAgentStateAsync(agent), $"QueryAgentState({agent})");
            return ret.getQueryAgentStateResult;
        }

        public static async Task<SnapshotCallEventReturn> SnapshotCallReqAsync(ConnectionID_t call)
        {
            try
            {
                var ret = await GetInstance().Exec(s => s.setSnapshotCallReqAsync(call),
                            $"SnapshotCallReqAsync({call.callID})");
                return ret;
            }
            catch (Exception)
            {
                throw new CstaException(CSTAUniversalFailure_t.invalidCstaDeviceIdentifier);
            }
        }

        
    }
}
