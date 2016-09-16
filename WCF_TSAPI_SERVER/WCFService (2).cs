using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using log4net;

namespace WCF_TSAPI_SERVER
{




    class WcfService:IDisposable
    {
        public readonly ILog Log = LogManager.GetLogger(typeof(WcfService)); 
        private readonly ServiceHost _hosts;
        public WcfService(Type type,String ip,String ipTest, int port,int maxRequest = 100) 
        {
             var bind = new NetTcpBinding(SecurityMode.None)
            {
                ListenBacklog = maxRequest * maxRequest,
                MaxConnections = maxRequest * maxRequest,
                SendTimeout = new TimeSpan(0, 1, 0),
                ReceiveTimeout = new TimeSpan(48, 0, 0),
                ReliableSession = {InactivityTimeout = new TimeSpan(24, 0, 0)}
            };
            var address = Dns.GetHostAddresses(Dns.GetHostName());
            if (address.All(t => t.ToString() != ipTest))
            {
                _hosts = new ServiceHost(type, new Uri(String.Format("http://{0}:{1}/", ip, port)));
                 _hosts.AddServiceEndpoint(type, bind, String.Format("net.tcp://{0}:{1}/", ip, port+1));
            }
            else
            {
                _hosts = new ServiceHost(type, new Uri(String.Format("http://{0}:{1}/", ipTest, port)));
                _hosts.AddServiceEndpoint(type, bind, String.Format("net.tcp://{0}:{1}/", ipTest, port + 1));
            }

            _hosts.Closed += _hosts_Closed;
            _hosts.Closing += _hosts_Closing;
            _hosts.Faulted += _hosts_Faulted;
            _hosts.UnknownMessageReceived += _hosts_UnknownMessageReceived;
            ThreadPool.SetMaxThreads(1000, 500);
            ThreadPool.SetMinThreads(10, 5);

            _hosts.Description.Behaviors.Add(
                    new ServiceThrottlingBehavior
                    {

                        MaxConcurrentCalls = maxRequest * maxRequest,
                        MaxConcurrentInstances = Int32.MaxValue,
                        MaxConcurrentSessions = maxRequest,
                       }
            );
            _hosts.Description.Behaviors.Add(
                   new ServiceMetadataBehavior
                   {
                       HttpGetEnabled = true

                   }
            );
           
       }

        void _hosts_UnknownMessageReceived(object sender, UnknownMessageReceivedEventArgs e)
        {
            Log.Error("_hosts_UnknownMessageReceived"+e.Message);
        }

        void _hosts_Faulted(object sender, EventArgs e)
        {
            Log.Error("_hosts_Faulted" + e);
        }

        void _hosts_Closing(object sender, EventArgs e)
        {
            Log.Debug("_hosts_Closing" + e);
        }

        void _hosts_Closed(object sender, EventArgs e)
        {
            Log.Debug("_hosts_Closed" + e);
        }
        public void Open()
        {
            _hosts.Open();
        }

        public void Close()
        {
            _hosts.Close();
        }

        public void Dispose()
        {
           Close();
           

        }
    }

}
