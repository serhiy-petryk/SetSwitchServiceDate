using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;

namespace TestMainService
{
    public partial class TestMainService : ServiceBase
    {
        private const string LogFileName = "E:\\Temp\\TestMainServiceLog.txt";

        public TestMainService()
        {
            InitializeComponent();
            ServiceName = "TestMainService";
        }

        protected override void OnStart(string[] args)
        {
            if (args.Length == 0)
            {
                var iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "args.ini");
                var argsExists = File.Exists(iniFilePath);
                args = File.ReadAllLines(iniFilePath).Where(a => !a.StartsWith("#")).ToArray();
            }

            // Update the service state to Start Pending.
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            var argString = string.Join(" ", args);
            // WriteToLog($"Started, {AppDomain.CurrentDomain.BaseDirectory} {Environment.CurrentDirectory} {System.Reflection.Assembly.GetEntryAssembly().Location} {Process.GetCurrentProcess().MainModule.FileName} {argsExists} args: {argString}");
            var childService = args.Length > 0 ? GetService(args[0]) : null;
            var childServiceStatus = childService?.Status.ToString() ?? "No service";
            WriteToLog($"Started, args: {argString}; service status: {childServiceStatus}");

            if (childService != null && childService.Status == ServiceControllerStatus.Stopped)
            {
                DateTime? date = null;
                if (args.Length > 1)
                {
                    var dateOk =
                        DateTime.TryParseExact(args[2], "yyyy-MM-dd", null, DateTimeStyles.None, out var date2);
                    if (dateOk) date = date2;
                }

                childService.Start();
                childService.WaitForStatus(ServiceControllerStatus.Running);
                // System.Threading.Thread.Sleep(5000);
                WriteToLog($"Service {childService.ServiceName} started");
            }

            // Update the service state to Running.
            WriteToLog($"On Start: BeforeEnd");
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            WriteToLog($"On Start: AfterEnd");
        }

        protected override void OnStop()
        {
        }

        private void WriteToLog(string message)
        {
            if (Directory.Exists(Path.GetDirectoryName(LogFileName)))
                File.AppendAllText(LogFileName, $"TestMainService: {message}; {DateTime.Now:O} {IsAdministrator()}" + Environment.NewLine);
        }

        private static string IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Admin" : "No admin";
            }
        }

        private static ServiceController GetService(string serviceName) => ServiceController.GetServices()
            .FirstOrDefault(a => string.Equals(a.ServiceName, serviceName));

        //============================================
        //============================================
        //============================================
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        public class StatusHelper
        {
            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        }

    }
}
