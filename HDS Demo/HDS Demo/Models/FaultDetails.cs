using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_Demo.Models
{
    public class FaultActions
    {
        public List<string> Capture { get; set; } = new();
        public List<string> CheckECUs { get; set; } = new();
        public List<string> Environment { get; set; } = new();
        public string? LogFileLocation { get; set; }
    }

    public class FaultEnvironment
    {
        public string Cpu { get; set; } = string.Empty;
        public string Ram { get; set; } = string.Empty;
        public string Gpu { get; set; } = string.Empty;
        public string Disk { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
    }

    //public class TimedValue<T>
    //{
    //    public DateTime Timestamp { get; set; }
    //    public T Value { get; set; }
    //}
}
