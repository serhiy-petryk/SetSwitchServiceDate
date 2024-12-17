using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

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
            WriteToLog($"{Environment.NewLine}");

            if (args.Length == 0)
            {
                var iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "args.ini");
                var argsExists = File.Exists(iniFilePath);
                args = File.ReadAllLines(iniFilePath).Where(a => !a.StartsWith("#")).ToArray();
            }
            var argString = string.Join(" ", args);

            // Find child service
            var childService = args.Length > 0 ? GetService(args[0]) : null;
            var childServiceStatus = childService?.Status.ToString() ?? "No service";
            WriteToLog($"Started, args: {argString}; service status: {childServiceStatus}");
            if (childService == null) return;

            DateTime? atDate = null;
            if (args.Length > 1)
            {
                var dateOk = DateTime.TryParseExact(args[1], "yyyy-MM-dd", null, DateTimeStyles.None, out var date2);
                if (dateOk) atDate = date2;
            }

            var dayDiff = atDate.HasValue ? Convert.ToInt32((DateTime.UtcNow - atDate.Value).TotalDays) : (int?)null;

            // Update the service state to Start Pending.
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 20000
            };
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Update the service state to Running.
            WriteToLog($"On Start: BeforeEnd");
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Task.Run(() =>
            {
                WriteToLog($"Task.Run. Status: {childService.Status}");
                if (childService.Status == ServiceControllerStatus.Stopped)
                {
                    // if (childService.StartType != ServiceStartMode.Automatic)
                    {
                        childService.Start();
                    }

                    childService.WaitForStatus(ServiceControllerStatus.Running);
                    WriteToLog($"Service {childService.ServiceName} started. Day difF: {dayDiff}");
                }
                WriteToLog($"Task.Run. End code. Status {childService.Status}");
            });

            WriteToLog($"On Start: AfterEnd");
        }

        /*protected override void OnStop()
        {
        }*/

        private void WriteToLog(string message)
        {
            if (Directory.Exists(Path.GetDirectoryName(LogFileName)))
            {
                if (string.IsNullOrEmpty(message.Trim()))
                    File.AppendAllText(LogFileName, message);
                else
                    File.AppendAllText(LogFileName, $"TestMainService: {message}; {DateTime.Now:O} {IsAdministrator()}" + Environment.NewLine);
            }
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

        //==============================
        //==============================
        //==============================
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                Year = (ushort)dt.Year;
                Month = (ushort)dt.Month;
                DayOfWeek = (ushort)dt.DayOfWeek;
                Day = (ushort)dt.Day;
                Hour = (ushort)dt.Hour;
                Minute = (ushort)dt.Minute;
                Second = (ushort)dt.Second;
                Milliseconds = (ushort)dt.Millisecond;
            }
        }

        // SYSTEMTIME systime = new SYSTEMTIME(date);
        // SetSystemTime(ref systime);
        [DllImport("kernel32.dll")]
        static extern bool SetSystemTime(ref SYSTEMTIME time);
    }
}
