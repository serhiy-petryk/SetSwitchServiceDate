using System.ServiceProcess;

namespace TestService
{
    static class Program
    {
        // https://learn.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer
        // https://www.c-sharpcorner.com/article/create-windows-services-in-c-sharp/
        //Install:      I:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe E:\Apps\project-2023\SetSwitchServiceDate\SwitchServiceManager\TestService\bin\Debug\TestService.exe
        //Uninstall:    I:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe -u E:\Apps\project-2023\SetSwitchServiceDate\SwitchServiceManager\TestService\bin\Debug\TestService.exe

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase.Run(new TestService());
        }
    }
}
