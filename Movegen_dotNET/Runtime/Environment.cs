using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Runtime;

public class Environment
{
    public static string GetProcessorName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
            return key?.GetValue("ProcessorNameString")?.ToString() ?? "Not Found";
        }

        return "Not Found";
    }
}
