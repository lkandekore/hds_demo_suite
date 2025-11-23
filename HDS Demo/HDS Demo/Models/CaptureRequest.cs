using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_Demo.Models
{
    public class CaptureRequest    {
        public string LogFileLocation { get; set; } = string.Empty;

        // ["DLTLogs", "PCAP", "ThreadDump", ...]
        public List<string> Capture { get; set; } = new();

        // ["CPU", "RAM", "NETWORK", "THREADS", "DISK"]
        public List<string> Environment { get; set; } = new();
    }
}
