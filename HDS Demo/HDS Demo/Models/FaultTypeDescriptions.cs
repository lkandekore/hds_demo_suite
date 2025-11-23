using System.Collections.Generic;

namespace HDS_Demo.Models
{
    public static class FaultTypeDescriptions
    {
        public static readonly Dictionary<string, string> Map =
            new Dictionary<string, string>
            {
                ["F0"] = "Software runtime exception",
                ["F1"] = "Stack/memory boundary violation",
                ["F2"] = "Task execution timeout",
                ["F3"] = "State machine logic error",
                ["F4"] = "Invalid or missing configuration",
                ["F5"] = "Flashing or update error",
                ["F6"] = "Memory or CPU resource exhaustion",
                ["F7"] = "IPC or messaging error",
                ["F8"] = "Real-time scheduling violation",
                ["F9"] = "Memory access violation",
                ["FA"] = "Integrity check failure",
                ["FB"] = "Unhandled exception or fault",
                ["FC"] = "Startup sequence error",
                ["FD"] = "Service not responding",
                ["FE"] = "Internal software supervision failure"
            };
    }
}
