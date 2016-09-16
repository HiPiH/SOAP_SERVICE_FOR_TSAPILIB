using WCF_TSAPI_CLIENT.TsapiServer;
using Exception = System.Exception;

namespace WCF_TSAPI_CLIENT
{
    public class CstaException:Exception
    {
        public CSTAUniversalFailure_t CustomError { get; set; }

        public CstaException(CSTAUniversalFailure_t customError):base(customError.ToString())
        {
            CustomError = customError;
        }
    }
}