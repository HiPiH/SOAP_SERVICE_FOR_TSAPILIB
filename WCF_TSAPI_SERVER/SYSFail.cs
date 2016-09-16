using System;
using System.Runtime.Serialization;

namespace WCF_TSAPI_SERVER
{
    [DataContract] 
    public class SYSFail
    {
        [DataMember]
        public Exception CustomError; 
        public SYSFail()
        {   
        }
        public SYSFail(Exception error)
        {   
            CustomError = error;   
        }   
    }
}