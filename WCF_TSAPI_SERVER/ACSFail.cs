using System.Runtime.Serialization;
using TSAPILIB2;

namespace WCF_TSAPI_SERVER
{
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
}