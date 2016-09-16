using System.ServiceProcess;
using WCF_TSAPI_SERVER.Properties;

namespace WCF_TSAPI_SERVER
{
    partial class Service1 : ServiceBase
    {
        WcfService _host ;
        public Service1()
        {
            ServiceName = Settings.Default.service_name;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _host?.Close();

            _host = new WcfService(typeof(TsapiServer), Settings.Default.ip_server,
                Settings.Default.ip_test,Settings.Default.service_port);
            _host.Open();
        }

        protected override void OnStop()
        {
            if (_host == null) return;
            _host.Close();
            _host = null;
        }
    }
}
