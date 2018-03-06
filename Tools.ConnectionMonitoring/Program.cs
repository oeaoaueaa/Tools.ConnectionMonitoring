using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using log4net.Config;

namespace Tools.ConnectionMonitoring
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var fileInfo = new FileInfo(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));  
            XmlConfigurator.Configure(fileInfo);

            var servicesToRun = new ServiceBase[]
            {
                new TcpConnectionMonitoringService()
            };

            if (Environment.UserInteractive)
            {
                var type = typeof(TcpConnectionMonitoringService);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

                var onStart = type.GetMethod("OnStart", flags);

                foreach (var service in servicesToRun)
                {
                    onStart.Invoke(service, new object[] { null });
                }

                Console.Title = "TcpConnectionMonitoring Service";
                Console.WriteLine("... Press [Enter] to stop service");
                Console.ReadLine();

                var onStop = type.GetMethod("OnStop", flags);

                foreach (var service in servicesToRun)
                {
                    onStop.Invoke(service, null);
                }
            }
            else
            {
                ServiceBase.Run(servicesToRun);
            }            
        }        
    }


}
