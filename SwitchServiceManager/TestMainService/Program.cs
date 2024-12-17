using System.ServiceProcess;

namespace TestMainService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new TestMainService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
