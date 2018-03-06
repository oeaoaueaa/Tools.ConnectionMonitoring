using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using log4net;

namespace Tools.ConnectionMonitoring
{
    partial class TcpConnectionMonitoringService : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TcpConnectionMonitoringService));

        public TcpConnectionMonitoringService()
        {
            InitializeComponent();
        }


        private bool _stop = false;

        protected override void OnStart(string[] args)
        {
            try
            {  
                var minimumConectionCount = int.Parse(ConfigurationManager.AppSettings["MiniumConnectionCount"]);
                var monitorIntervalInSeconds = int.Parse(ConfigurationManager.AppSettings["MonitorIntervalInSeconds"]);

                Log.Info($"Start MiniumConnectionCount={minimumConectionCount} MonitorIntervalInSeconds={monitorIntervalInSeconds}");

                string SimpleProcessName(string processName) =>
                    processName.Replace(".", string.Empty);

                var monitorTimer = new Timer(monitorIntervalInSeconds * 1000);
                monitorTimer.AutoReset = false;
                monitorTimer.Elapsed += (sender, eventArgs) =>
                {
                    try
                    {
                        if (!_stop)
                        {
                            var connections = SocketConnections.GetAllTcpConnections();

                            var connectionsByProcessName = connections.GroupBy(c => c.ProcessName)
                                .Where(g => g.Count() >= minimumConectionCount)
                                .OrderBy(g => g.Key).ToList();

                            var connectionsAndProcesses =
                                connectionsByProcessName
                                    .Select(g =>
                                        g.OrderBy(c => c.RemotePort)
                                            .Select(c => $"{c.ProcessName}={c.RemoteAddress}:{c.RemotePort} {c.State}").ToList())
                                    .SelectMany(s => s).ToList();

                            var connectionsTotals =
                                connectionsByProcessName
                                    .Select(g => new
                                    {
                                        processName = g.Key,
                                        totals = g.GroupBy(c => c.RemotePort).ToList()
                                    })
                                    .Select(p =>
                                        string.Join(", ",
                                            p.totals.Where(t => t.Count() > minimumConectionCount)
                                                .Select(c => $"{SimpleProcessName(p.processName)}{c.Key}={c.Count()}").ToList()));

                            Log.Info(string.Join(Environment.NewLine, connectionsTotals));
                            Log.Debug(string.Join(Environment.NewLine, connectionsAndProcesses));

                            monitorTimer.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Timer error", ex);
                    }
                };
                monitorTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Error("unexpected error", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            _stop = true;
        }
    }
}
