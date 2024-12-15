using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;

namespace SwitchServiceManager
{
    // Command line:
    // E:\Apps\project-2023\SetSwitchServiceDate\SwitchServiceManager\SwitchServiceManager\bin\Debug\SwitchServiceManager.exe on TestService 2024-05-07 E:\Temp\SwitchServiceManagerLog.txt

    class Program
    {
        private static string _logFileName;

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "?" || args[0] == "/?" || args.Length<3)
            {
                Console.WriteLine($"Startup or shutdown the service at the specified date. The program result saves in Window application log.");
                Console.WriteLine();
                Console.WriteLine($"The program must run in ADMINISTRATOR (!!!) mode");
                Console.WriteLine();
                Console.WriteLine($"SwitchServiceManager.exe on|off ServiceName date [LogFileName]");
                Console.WriteLine();
                Console.WriteLine($"    on|off          startup (on) or shutdown (off) the service");
                Console.WriteLine($"    ServiceName     service name (not display name !!!)");
                Console.WriteLine($"    date            date in format yyyy-MM-dd when to startup/shutdown the service, e.g., 2024-01-03 means 2024, January 3");
                Console.WriteLine($"    [LogFileName]   (optional) full path to log file name");
                return;
            }

            bool? startup = null;
            if (string.Equals(args[0], "on", StringComparison.InvariantCultureIgnoreCase))
                startup = true;
            else if (string.Equals(args[0], "off", StringComparison.InvariantCultureIgnoreCase))
                startup = false;

            var service = args.Length > 2 ? GetService(args[1]) : null;
            var dateOk = DateTime.TryParseExact(args[2], "yyyy-MM-dd", null, DateTimeStyles.None, out var date);
            _logFileName = args.Length > 3 ? args[3] : null;
            var logFileNameOk = string.IsNullOrEmpty(_logFileName) || Directory.Exists(Path.GetDirectoryName(_logFileName));

            if (startup.HasValue && service != null && dateOk && logFileNameOk)
            {
                if (!string.IsNullOrEmpty(_logFileName))
                    File.AppendAllText(_logFileName, Environment.NewLine);

                try
                {
                    var projectName = Assembly.GetCallingAssembly().GetName().Name;
                    if (startup.Value)
                    {
                        StartServiceAtDate(service, date);
                        WriteToTextLog($"Started");
                        WriteToApplicationLog($"{projectName}: '{service.ServiceName}' service started at {date:yyyy-MM-dd}");
                    }
                    else
                    {
                        StopServiceAtDate(service, date);
                        WriteToTextLog($"Stopped");
                        WriteToApplicationLog($"{projectName}: '{service.ServiceName}' service shutdown at {date:yyyy-MM-dd}");
                    }
                }
                catch (Exception ex)
                {
                    WriteToTextLog("ERROR: " + ex.Message);
                }

            }
            else // invalid arguments
            {
                WriteToTextLog("ERROR: invalid arguments");
            }
        }

        private static void WriteToApplicationLog(string message)
        {
            using (var eventLog = new EventLog("Application"))
            {
                eventLog.Source = ".NET Runtime";
                eventLog.WriteEntry(message, EventLogEntryType.Information, 1000);
            }
        }

        private static void WriteToTextLog(string message)
        {
            if (!string.IsNullOrEmpty(_logFileName))
                File.AppendAllText(_logFileName, $"SwitchServiceManager: {message} {DateTime.Now:O} {IsAdministrator()}" + Environment.NewLine);
        }

        private static string IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Admin" : "No admin";
            }
        }

        private static void StartServiceAtDate(ServiceController service, DateTime atDate)
        {
            if (service != null && !CheckServiceStatus(service, ServiceControllerStatus.Running))
            {
                WriteToTextLog($"BeforeStarted");
                var dayDiff = Convert.ToInt32((DateTime.UtcNow - atDate).TotalDays);
                var systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(-dayDiff));
                SetSystemTime(ref systime);

                WriteToTextLog($"BeforeTestStarted");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
                System.Threading.Thread.Sleep(5000);
                WriteToTextLog($"AfterTestStarted");

                systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(dayDiff));
                SetSystemTime(ref systime);
                WriteToTextLog($"AfterStarted");
            }
        }

        private static void StopServiceAtDate(ServiceController service, DateTime atDate)
        {
            if (service != null && !CheckServiceStatus(service, ServiceControllerStatus.Stopped))
            {
                WriteToTextLog($"BeforeStop");
                var dayDiff = Convert.ToInt32((DateTime.UtcNow - atDate).TotalDays);
                var systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(-dayDiff));
                SetSystemTime(ref systime);

                WriteToTextLog($"BeforeTestStopped");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
                System.Threading.Thread.Sleep(1000);
                WriteToTextLog($"AfterTestStopped");

                systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(dayDiff));
                SetSystemTime(ref systime);
                WriteToTextLog($"AfterStopped");
            }
        }

        private static ServiceController GetService(string serviceName) => ServiceController.GetServices()
            .FirstOrDefault(a => string.Equals(a.ServiceName, serviceName));

        private static bool CheckServiceStatus(ServiceController service, ServiceControllerStatus requiredStatus)
        {
            ServiceControllerStatus status;
            uint counter = 0;
            do
            {
                if (service == null)
                    return false;

                Thread.Sleep(100);
                status = service.Status;
            } while (!(status == ServiceControllerStatus.Stopped ||
                       status == ServiceControllerStatus.Running) &&
                     (++counter < 30));
            return status == requiredStatus;
        }

        //============================================
        //============================================
        //============================================

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
