using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using WCF_TSAPI_CLIENT.Monitors;
using WCF_TSAPI_CLIENT.TsapiServer;
using Exception = System.Exception;

namespace WCF_TSAPI_CLIENT
{

    internal class WcfProxy : IDisposable
    {
        private const int WaitMs = 5000;
        private readonly int _id;
        private readonly ILog _log = LogManager.GetLogger(typeof (WcfProxy));
        internal readonly TsapiMonitors Monitors;
        private readonly AutoResetEvent _waitClient = new AutoResetEvent(true);
        private readonly AutoResetEvent _waitCommand = new AutoResetEvent(true);
        private TsapiServerClient _client;
        private readonly object _lockConnector = new object();
        private static readonly CSTAUniversalFailure_t[] ErrorForReboot =
        {
            CSTAUniversalFailure_t.serviceBusy,
            CSTAUniversalFailure_t.CBTaskIsFull,
            CSTAUniversalFailure_t.acsHandleTerminationRejection,
            CSTAUniversalFailure_t.genericOperation,
            CSTAUniversalFailure_t.genericPerformanceManagement,
            CSTAUniversalFailure_t.genericOperationRejection,
            CSTAUniversalFailure_t.genericStateIncompatibility,
            CSTAUniversalFailure_t.genericUnspecified,
            CSTAUniversalFailure_t.genericUnspecifiedRejection,
            CSTAUniversalFailure_t.genericSubscribedResourceAvailability,
            CSTAUniversalFailure_t.initiatorReleasingRejection,
            CSTAUniversalFailure_t.requestTimeoutRejection,
            CSTAUniversalFailure_t.resourceOutOfService,
            CSTAUniversalFailure_t.acsHandleTerminationRejection,
            CSTAUniversalFailure_t.networkBusy,
            CSTAUniversalFailure_t.networkOutOfService,
            CSTAUniversalFailure_t.serviceTerminationRejection
        };


        public WcfProxy(int id)
        {
            _id = id;
            Monitors = new TsapiMonitors();
            Monitors.OnServerFail += MonitorsOnOnServerFail;
            _log.Debug($"{_id,6}; CreateInstance");
        }

        private void MonitorsOnOnServerFail()
        {
            OpenConnect();
        }


        public TsapiServerClient OpenConnect()
        {
            switch (_client?.State)
            {
                case CommunicationState.Opened:
                    return _client;
                case CommunicationState.Opening:
                    _waitClient.WaitOne(WaitMs);
                    return _client;
            }
            lock (_lockConnector)
            {
             
                _client = new TsapiServerClient(new InstanceContext(Monitors));
                _client.InnerDuplexChannel.Closed += InnerChannelOnClosed;
                _client.InnerDuplexChannel.Opened += InnerChannelOnOpened;
                _client.InnerDuplexChannel.Faulted += InnerChannelOnFaulted;
                try
                {
                    _client.Open();
                   
                }
                catch (EndpointNotFoundException ex)
                {
                    _client = null;
                    _log.Error(ex);
                    Environment.Exit(-1001); // CtriticalError
                } 
                
            }
            _waitClient.WaitOne(WaitMs);
            
            return _client;
        }

        internal bool CstaExpetion(FaultException<CSTAFail> csta, string command)
        {
           
            if (csta.Detail.CustomError == CSTAUniversalFailure_t.CBTaskIsFull ||
                        csta.Detail.CustomError == CSTAUniversalFailure_t.operationTimeout)
            {
          
                _log.Error($"{_id,6}; {command} {csta.Detail.CustomError}");
                Task.Delay(1000).Wait();
                return true;
            }


            if (!ErrorForReboot.Contains(csta.Detail.CustomError))
            {
                _log.Debug($"{_id,6}; {command} {csta.Detail.CustomError}");
                throw new CstaException(csta.Detail.CustomError);
            }

            return false;
        }
        internal async Task<TT> Exec<TT>(Func<TsapiServerClient, Task<TT>> func, string command,int x = 0)
        {
            
            for (; x < 3; x++)
            {
                Task.Delay(50).Wait();
                var connect = OpenConnect();
                _waitCommand.WaitOne(WaitMs);
                try
                {
                    if (connect.State != CommunicationState.Opened)
                    {
                        _waitCommand.Set();
                        continue;
                    }
                    var ret = func(connect);
                    _waitCommand.Set();
                    _log.Debug($"{_id,6}; {command} - ok ");
                    return await ret;
                }
                catch (FaultException<CSTAFail> csta)
                {
                    _log.Error($"{_id,6}; '{command}'; FaultException<CSTAFail> ");
                    if (CstaExpetion(csta, command))
                        continue;
                    Environment.Exit(-1002); // Full reboot
                    throw;
                }
                catch (CommunicationObjectFaultedException)
                {
                    _log.Error($"{_id,6}; '{command}'; CommunicationObjectFaultedException ");
                }
                catch (Exception ex) //CommunicationObjectFaultedException,CommunicationException
                {
                    _log.Error($"{_id,6}; {command} ", ex);
                }
                finally
                {
                    _waitCommand.Set();
                }
            }
            throw new Exception($" '{command}'; Error execute command");
        }

 

        private void InnerChannelOnOpened(object sender, EventArgs eventArgs)
        {
            _log.Debug($"{_id,6}; ChannelOnOpened");
            _waitClient.Set();
            _waitCommand.Set();
            Monitors.Reset();
        }


        private void InnerChannelOnFaulted(object sender, EventArgs eventArgs)
        {
            _log.Error($"{_id,6}; ChannelOnFaulted");
            Task.Delay(500).Wait();
            OpenConnect();
        }


        private void InnerChannelOnClosed(object sender, EventArgs eventArgs)
        {
            _log.Debug($"{_id,6}; ChannelOnClosed");
        }



        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Close();
            }

        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void CloseConnect()
        {
            _client.Close();
        }
    }
}
