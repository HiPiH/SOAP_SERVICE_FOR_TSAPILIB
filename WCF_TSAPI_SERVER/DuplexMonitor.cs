using TSAPILIB2;

namespace WCF_TSAPI_SERVER
{
    struct DuplexMonitor
    {
        public ConnectionID_t Target;
        public IDuplexCallback Callback;
        public TypeMonitor Type;
    }
}