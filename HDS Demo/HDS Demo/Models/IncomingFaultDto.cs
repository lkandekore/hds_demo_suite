using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_Demo.Models
{
    public class IncomingFaultDto
    {
        public string FaultCode { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public CaptureRequest CaptureRequest { get; set; }
    }

}
