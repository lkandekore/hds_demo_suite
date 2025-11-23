using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_Demo.Models
{
    public class FaultTimeSeries
    {
        public List<TimedValue<float>> Cpu { get; set; } = new();
        public List<TimedValue<float>> Ram { get; set; } = new();
        public List<TimedValue<float>> Gpu { get; set; } = new();
        public List<TimedValue<float>> Disk { get; set; } = new();
        public List<TimedValue<float>> Network { get; set; } = new();
    }
    public class TimedValue<T>
    {
        public DateTime Timestamp { get; set; }
        public T Value { get; set; }
    }
}
