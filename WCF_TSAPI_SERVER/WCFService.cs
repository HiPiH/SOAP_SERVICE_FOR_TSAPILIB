using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using log4net;

namespace WCF_TSAPI_SERVER
{
    

    public class Log4NetErrorHandler : IErrorHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool HandleError(Exception error)
        {
            if (error is CommunicationObjectAbortedException)
            {

            }
            else
            {
                Log.Error("An unexpected has occurred.", error);
            }

            return false; // Exception has to pass the stack further
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
        }
    }
    

    public class Log4NetBehaviorExtensionElement : BehaviorExtensionElement, IServiceBehavior
    {
        public override Type BehaviorType
        {
            get { return typeof(Log4NetBehaviorExtensionElement); }
        }

        protected override object CreateBehavior()
        {
            return new Log4NetBehaviorExtensionElement();
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        {
           
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            IErrorHandler errorHandler = new Log4NetErrorHandler();

            foreach (var channelDispatcherBase in serviceHostBase.ChannelDispatchers)
            {
                var channelDispatcher = (ChannelDispatcher) channelDispatcherBase;
                channelDispatcher.ErrorHandlers.Add(errorHandler);
            }
        }
    }

    class WcfService:IDisposable
    {
        private readonly string _ipTest;
        public readonly ILog Log = LogManager.GetLogger(typeof(WcfService)); 
        private readonly ServiceHost _hosts;
        public WcfService(Type type,string ip,string ipTest, int port,int maxRequest = 100) 
        {
            _ipTest = ipTest;
            var bind = new NetTcpBinding(SecurityMode.None)
            {
                ListenBacklog = maxRequest * maxRequest,
                MaxConnections = maxRequest * maxRequest,
                SendTimeout = new TimeSpan(0, 2, 0),
                ReceiveTimeout = new TimeSpan(48, 0, 0),
                ReliableSession = {InactivityTimeout = new TimeSpan(24, 0, 0)}
            };
            var address = Dns.GetHostAddresses(Dns.GetHostName());
            if (address.All(t => t.ToString() != ipTest))
            {
//IPAddress.Any
                _hosts = new ServiceHost(type, new Uri($"http://{ip}:{port}/"));
                _hosts.AddServiceEndpoint(type, bind, $"net.tcp://{ip}:{port + 1}/");
                
            }
            else
            {
                _hosts = new ServiceHost(type, new Uri($"http://{ipTest}:{port}/"));
                _hosts.AddServiceEndpoint(type, bind, $"net.tcp://{ipTest}:{port + 1}/");
                
                
            }

            
            _hosts.Closed += _hosts_Closed;
            _hosts.Closing += _hosts_Closing;
            _hosts.Faulted += _hosts_Faulted;
            _hosts.UnknownMessageReceived += _hosts_UnknownMessageReceived;
            
            _hosts.ChannelDispatchers.ToList().ForEach(t =>
            {
                
                t.Closed += (sender, args) => Log.Debug("ChannelDispatchers.Closed");
                t.Closing += (sender, args) => Log.Debug("ChannelDispatchers.Closing");
                t.Faulted  += (sender, args) => Log.Debug("ChannelDispatchers.Faulted");
                t.Opened += (sender, args) => Log.Debug("ChannelDispatchers.Opened");
                t.Opening += (sender, args) => Log.Debug("ChannelDispatchers.Opening");
            });



            ThreadPool.SetMaxThreads(1000, 500);
            ThreadPool.SetMinThreads(10, 5);
            _hosts.Description.Behaviors.Add(new Log4NetBehaviorExtensionElement());
            _hosts.Description.Behaviors.Add(
                    new ServiceThrottlingBehavior
                    {
                        MaxConcurrentCalls = maxRequest * maxRequest,
                        MaxConcurrentInstances = int.MaxValue,
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
            Log.Debug("_hosts_signal open");
            _hosts.Open();
        }

        public void Close()
        {
            Log.Debug("_hosts_signal close");
            _hosts.Close();
        }

        public void Dispose()
        {
            Log.Debug("_hosts_signal dispose");
           Close();
           

        }
    }

}
