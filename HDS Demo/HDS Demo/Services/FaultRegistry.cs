using System;
using System.Collections.Generic;
using System.Linq;
using HDS_Demo.Models;

namespace HDS_Demo.Server
{
    public class FaultRegistry
    {
        // Key = (App, FaultCode, Type)
        private readonly Dictionary<(string App, string Code, string Type), FaultSignature> _faults = new();


        public event Action<FaultSignature> FaultReported;

        // Human-readable type descriptions
        private static readonly Dictionary<string, string> FaultTypeDescriptions =
            new(StringComparer.OrdinalIgnoreCase)
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

        public FaultRegistry() { }

        // ============================================================
        // Add or update fault — Called by /faults/report
        // ============================================================

        public FaultSignature Add(FaultSignature fault)
        {
            // Normalize description
            if (FaultTypeDescriptions.TryGetValue(fault.Type, out string desc))
                fault.TypeDescription = desc;
            else
                fault.TypeDescription = "Unknown type";

            fault.LastTimestamp = fault.Timestamp;

            var key = (fault.ApplicationName, fault.FaultCode, fault.Type);

            if (_faults.TryGetValue(key, out var existing))
            {
                // Repeated fault instance
                existing.Count++;
                existing.LastTimestamp = fault.Timestamp;

                // Notify listeners (ViewModel)
                FaultReported?.Invoke(existing);

                return existing;
            }

            // Brand new fault
            if (fault.FaultId == null)
                fault.FaultId = Guid.NewGuid();

            fault.Count = 1;
            _faults[key] = fault;

            // Notify listeners (ViewModel)
            FaultReported?.Invoke(fault);

            return fault;
        }

        // ============================================================
        // Get current snapshot of faults
        // ============================================================

        public List<FaultSignature> GetAll()
        {
            return _faults.Values.ToList();
        }

        // Optionally: Clear faults
        public void Clear()
        {
            _faults.Clear();
        }
    }
}
