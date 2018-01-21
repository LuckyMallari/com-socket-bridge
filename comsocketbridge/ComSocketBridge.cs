using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ComSocketBridge
{
    class ComSocketBridge : ServiceBase
    {
        const string SERVICENAME = "ComSocket Bridge";
        const string DESCRIPTION = "ComSocket Bridge provides a two-way communication bridge between a COM/Serial PORT and TCP/IP";

        #region Private
        private TcpManager _tcpManager;
        private static SerialPortHelper _serialPort;
        private static readonly ManualResetEventSlim ShutdownEvent = new ManualResetEventSlim(false);
        private static readonly ComSocketBridge comsocketbridge = new ComSocketBridge();
        #endregion Private

        #region Public
        public static SerialPortHelper SerialPort => _serialPort;
        public static event ComSocketBridgeStartedHandler OnCom2SockStarted;
        public static event ComSocketBridgeOnBeforeStartHandler OnCom2SockOnBeforeStart;
        #endregion Public

        public ComSocketBridge()
        {
            Console.WriteLine(ConfigManager.Header.Replace("__YEAR__", DateTime.Now.Year.ToString()));

            if (Environment.GetCommandLineArgs().Length > 1 
                && string.Equals(Environment.GetCommandLineArgs()[1], "uninstall", StringComparison.CurrentCultureIgnoreCase))
            {
                Uninstall();
                return;
            }

            if (ConfigManager.IsRunAsService)
            {
                CheckService();
            }

            _serialPort = new SerialPortHelper(ConfigManager.ComPort, Logger.Log);
            _serialPort.OnSerialDataRecieved += TcpManager.Broadcast;
            _serialPort.OnSerialConnectFail += OnSerialConnectFail;
            _serialPort.OnSerialConnectSuccess += OnSerialConnectSuccess;

            _tcpManager = new TcpManager(ConfigManager.LocalPort, ConfigManager.Greeting);
            _tcpManager.OnListeningStarted += OnTcpListeningStarted;
            _tcpManager.OnListeningFailed += OnTcpListeningFailed;
            _tcpManager.OnReceivedFromClientDelegate += _serialPort.Send;
        }

        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (ShutdownEvent.IsSet)
                return;

            if (ConfigManager.IsRunAsService && !Environment.UserInteractive)
            {
                // Run as service
                Logger.Log("Running as a Service.");
                Run(new ServiceBase[] { comsocketbridge });
            }
            else
            {
                // Run as a standalone
                Logger.Log("Running as standalone. CTRL+C to stop!");
                comsocketbridge.OnStart(null);

                while (!ShutdownEvent.IsSet)
                {
                }
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Hit Enter to exit");
                Console.ReadLine();
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.Log(e.ExceptionObject.ToString());
            }
            catch
            {
                // ignored
            }
        }

        protected override void OnStop()
        {
            RequestAdditionalTime(30 * 1000);
            _serialPort.DisconnectSerial();
            Die();
            Logger.Log("Service stopped.");
            base.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log("Started");

            try
            {
                OnBeforeStart();
            }
            catch (Exception ex)
            {
                Logger.Log("OnBeforeStart failed: " + ex.Message);
                return;
            }

            _tcpManager.StartListener();
        }


        private void OnBeforeStart()
        {
            if (OnCom2SockOnBeforeStart == null)
                return;

            Logger.Log("Invoking ComSocketBridge OnBeforeStart..");
            OnCom2SockOnBeforeStart(this, null);
        }

        private static void ConnectToCom()
        {
            // Establish connection to serial port:
            Logger.Log($"Connecting to {_serialPort.ComPort}");
            _serialPort.Connect();
        }

        private static void OnSerialConnectFail(object sender, MessengerEventArgs args)
        {
            Logger.Log($"{_serialPort.ComPort} Connect failed.");
            if (ConfigManager.RetryInterval > -1)
            {
                Logger.Log($"Will retry COM in {ConfigManager.RetryInterval} ms.");
                var t = Task.Run(async delegate
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(ConfigManager.RetryInterval));
                    ConnectToCom();
                });
            }
        }

        private static void OnSerialConnectSuccess(object sender, MessengerEventArgs args)
        {
            Logger.Log($"{_serialPort.ComPort} connected.");
        }


        private static void OnTcpListeningFailed(object sender, MessengerEventArgs args)
        {
            Logger.Log(args.Message);
            Die();
        }

        private static void OnTcpListeningStarted(object sender, MessengerEventArgs args)
        {
            ConnectToCom();
        }

        private static void Die()
        {
            ShutdownEvent.Set();

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Hit ENTER to exit");
                Console.ReadLine();
            }
        }

        private static void CheckService()
        {
            ServiceController[] services = ServiceController.GetServices();
            var isInstalled = services.FirstOrDefault(s => s.ServiceName == SERVICENAME) != null;
            if (isInstalled)
                return;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;

            Logger.Log($"Installing service: {SERVICENAME}");
            var proc = Process.Start("SC", $"CREATE \"{SERVICENAME}\" binPath=\"{assembly}\" start=auto");
            proc.WaitForExit();

            proc = Process.Start("SC", $"DESCRIPTION \"{SERVICENAME}\" \"{DESCRIPTION}\"");
            proc.WaitForExit();

            Logger.Log($"Starting service...");
            proc = Process.Start("NET", $"START \"{SERVICENAME}\"");
            proc.WaitForExit();

            Logger.Log($"Service installed and started!");
            Die();
            Environment.Exit(1);
        }

        private static void Uninstall()
        {
            ServiceController[] services = ServiceController.GetServices();
            var isInstalled = services.FirstOrDefault(s => s.ServiceName == SERVICENAME) != null;
            if (isInstalled)
            {
                var proc = Process.Start("NET", $"STOP \"{SERVICENAME}\"");
                proc.WaitForExit();
                proc = Process.Start("SC", $"DELETE \"{SERVICENAME}\"");
            }
            Logger.Log($"Service stopped and uninstalled!");
            Die();
        }
    }


}

