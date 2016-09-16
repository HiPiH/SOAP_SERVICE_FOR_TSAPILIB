using System;
using System.Diagnostics;
using System.Threading;
using WCF_TSAPI_SERVER.Properties;
using System.ServiceProcess;
using log4net;
using TSAPILIB2;

namespace WCF_TSAPI_SERVER
{
    class Program
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(Program)); 
        static void Main(string[] args)
        {
         /*   var t = new Tsapi(Settings.Default.server, Settings.Default.login,
                            Settings.Default.password, "_main", Settings.Default.api,
                            Settings.Default.version);
t.OpenStream();
            t.SetSnapshotDeviceReq("4997").ContinueWith(task =>
            {

            });
            Console.Read();*/
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            log4net.Config.XmlConfigurator.Configure();
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "-c":


                        var host = new WcfService(typeof(TsapiServer),Settings.Default.ip_server,Settings.Default.ip_test, Settings.Default.service_port);
                         host.Open();
                         Console.WriteLine(@"....");
                         Console.ReadLine();
                         host.Close();




                        /*using (var t = new TsapiServer())
                        {
                            var retss = t.getQueryAcdSplit("7245");
                            retss.Wait(50000);

                            Console.ReadLine();

                        }*/

                        /*var host = new WcfService(typeof(TsapiServer),Settings.Default.ip_server,Settings.Default.ip_test, Settings.Default.service_port);
                               host.Open();

                                       Console.WriteLine(@"....");
                                  Console.ReadLine();
                                  host.Close();
                       
                        /*  var t = new TsapiServer();
                        var retss = t.getQueryAcdSplit("7245");
                        retss.Wait(50000);
                               .ContinueWith(
                                     task =>
                                         Console.WriteLine(task.Result.Att.agentsLoggedIn))*/

                        /*  Console.ReadLine();

                          ar s1 = t.setMakeCall("5202", "4997", "", false, new ATTV5UserToUserInfo_t());
                                 s1.Wait();
                                 Thread.Sleep(1000);
                                 Console.WriteLine(s1.Result.Att.ucid);
                                 var s2 = t.setAnswerCall(new ConnectionID_t(s1.Result.CSTA.newCall.callID,"4997"));
                                 s2.Wait();
                                 Console.WriteLine(@"....");
                                     Console.ReadLine();
                                                    t.Dispose();*/

                        break;
                    case "-i":
                    case "-u":
                        String frm = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + "installutil.exe";
                        String app = Process.GetCurrentProcess().MainModule.FileName;
                        Console.WriteLine(frm);
                        Console.WriteLine(app);
                        Console.WriteLine(@"Start");
                        Console.WriteLine($"============service name  = '{ Settings.Default.service_name}'==============");
                        var p = new Process
                        {
                            StartInfo =
                            {
                                UseShellExecute = false,
                                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                                RedirectStandardOutput = true,
                                FileName = frm,
                                Arguments = (args[0] == "-u" ? "/u " : " ") + "\"" + app + "\"  /LogToConsole=true"
                            }
                        };
                        Console.WriteLine(p.StartInfo.Arguments);
                        p.Start();
                        p.WaitForExit();
                        Console.WriteLine(p.ExitCode == 0 ? "Succeful" : "Failde");
                        Console.WriteLine(@"===============================================");
                        Console.WriteLine(@"End");
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new Service1());
            }

        }
      

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(sender, ((Exception) e.ExceptionObject));
        }

    
    }
}
