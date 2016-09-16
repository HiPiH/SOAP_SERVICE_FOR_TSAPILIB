using System.Runtime.Serialization;
using TSAPILIB2;

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
}