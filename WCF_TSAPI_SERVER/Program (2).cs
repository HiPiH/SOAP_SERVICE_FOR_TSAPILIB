using System;
using System.Diagnostics;
using WCF_TSAPI_SERVER.Properties;
using System.ServiceProcess;
using log4net;
namespace WCF_TSAPI_SERVER
{
    class Program
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(Program)); 
        static void Main(string[] args)
        {
      
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
                        
                        /*v                           var t = new TsapiServer();
                                                  while (true)
                                                  {
                                                      t.getQueryAgentLogin("7245").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                               
                                                      t.getQueryAgentLogin("7286").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7219").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7260").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7245").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7286").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7219").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                
                                                      t.getQueryAgentLogin("7260").ContinueWith(task => Console.WriteLine(String.Join(",", task.Result.ToArray())));
                                

                            
                                                  }
                                                  Console.ReadLine();
                            
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
                        Console.WriteLine(@"===============================================");
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
