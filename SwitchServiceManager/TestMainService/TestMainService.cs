using System;
using System.IO;
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
            // Update the service state to Start Pending.
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            var argString = string.Join(" ", args);
            WriteToLog($"Started, args: {argString}");

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            StatusHelper.SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
        }

        private void WriteToLog(string message)
        {
            if (Directory.Exists(Path.GetDirectoryName(LogFileName)))
                File.AppendAllText(LogFileName, $"TestMainService: {message} {DateTime.Now:O} {IsAdministrator()}" + Environment.NewLine);
        }

        private static string IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Admin" : "No admin";
            }
        }

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
