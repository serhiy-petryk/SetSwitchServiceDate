using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;

namespace SwitchServiceManager
{
    class ProgramOld
    {
        private const string LogFileName = "E:\\Temp\\MultiseatServiceManagerLog.txt";
        private static readonly DateTime RunDate = new DateTime(2024, 5, 7);
        private const string TestServiceName = "TestService";

        static void xMain(string[] args)
        {

            var startup = (args.Length > 0 && string.Equals(args[0], "Startup", StringComparison.InvariantCultureIgnoreCase));

            File.AppendAllText(LogFileName, Environment.NewLine);

            try
            {
                var projectName = Assembly.GetCallingAssembly().GetName().Name;
                if (startup)
                {
                    StartTestServiceUtc();
                    WriteToLog($"Started");
                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = ".NET Runtime";
                        eventLog.WriteEntry($"{projectName}: start service {TestServiceName}", EventLogEntryType.Information, 1000);
                        // eventLog.WriteEntry(".NET Runtime", $"{projectName}: start service {TestServiceName}", EventLogEntryType.Information, 1000);
                    }
                }
                else
                {
                    StopTestServiceUtc();
                    WriteToLog($"Stopped");
                    using (var eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = ".NET Runtime";
                        /*EventLog.WriteEntry(
                            ".NET Runtime", //magic
                            "Your error message goes here!!",
                            EventLogEntryType.Warning,
                            1000); //magic*/
                        eventLog.WriteEntry($"{projectName}: shutdown service {TestServiceName}", EventLogEntryType.Information, 1000);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToLog("ERROR: " + ex.Message);
            }
        }

        public static void WriteToLog(string message) => File.AppendAllText(LogFileName, $"MultiseatManagerService: {message} {DateTime.Now:O} {IsAdministrator()}" + Environment.NewLine);

        public static string IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Admin" : "No admin";
            }
        }

        private static void StartTestServiceUtc()
        {
            var testService = GetService(TestServiceName);
            if (testService != null && !CheckServiceStatus(TestServiceName, ServiceControllerStatus.Running))
            {
                WriteToLog($"BeforeStarted");
                var dayDiff = Convert.ToInt32((DateTime.UtcNow - RunDate).TotalDays);
                var systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(-dayDiff));
                SetSystemTime(ref systime);

                WriteToLog($"BeforeTestStarted");
                testService.Start();
                testService.WaitForStatus(ServiceControllerStatus.Running);
                System.Threading.Thread.Sleep(5000);
                WriteToLog($"AfterTestStarted");

                systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(dayDiff));
                SetSystemTime(ref systime);
                WriteToLog($"AfterStarted");
            }
        }

        private static void StopTestServiceUtc()
        {
            var testService = GetService(TestServiceName);
            if (testService != null && !CheckServiceStatus(TestServiceName, ServiceControllerStatus.Stopped))
            {
                WriteToLog($"BeforeStop");
                var dayDiff = Convert.ToInt32((DateTime.UtcNow - RunDate).TotalDays);
                var systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(-dayDiff));
                SetSystemTime(ref systime);

                WriteToLog($"BeforeTestStopped");
                testService.Stop();
                testService.WaitForStatus(ServiceControllerStatus.Stopped);
                System.Threading.Thread.Sleep(1000);
                WriteToLog($"AfterTestStopped");

                systime = new SYSTEMTIME(DateTime.UtcNow.AddDays(dayDiff));
                SetSystemTime(ref systime);
                WriteToLog($"AfterStopped");
            }
        }

        public static ServiceController GetService(string serviceName) => ServiceController.GetServices()
            .FirstOrDefault(a => string.Equals(a.ServiceName, serviceName));

        public static bool CheckServiceStatus(string serviceName, ServiceControllerStatus requiredStatus)
        {
            ServiceControllerStatus status;
            uint counter = 0;
            do
            {
                ServiceController service = GetService(serviceName);
                if (service == null)
                {
                    return false;
                }

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
