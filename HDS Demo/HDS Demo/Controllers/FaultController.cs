using HDS_Demo.Models;
using HDS_Demo.Server;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace HDS_Demo.Controllers
{
    [ApiController]
    [Route("api/v1/faults")]
    public class FaultController : ControllerBase
    {
        private readonly FaultRegistry _faults;
        private readonly HdsViewModel _viewModel;

        public FaultController(FaultRegistry faults, HdsViewModel viewModel)
        {
            _faults = faults;
            _viewModel = viewModel;
        }

        [HttpPost("report")]
        public IActionResult ReportFault([FromBody] FaultSignature fault)
        {
            if (fault == null)
                return BadRequest(new { error = "Invalid JSON" });

            if (fault.Timestamp == DateTime.MinValue)
                fault.Timestamp = DateTime.UtcNow;

            // Store in registry (adds count, type description, last timestamp)
            var stored = _faults.Add(fault);

            // Update UI fully (pass registry!)
            _viewModel.TriggerFault(stored, _faults);

            return Ok(new
            {
                status = "fault_recorded",
                fault_id = stored.FaultId,
                timestamp = stored.Timestamp
            });
        }

        [HttpGet("app/{application}")]
        public IActionResult GetFaultsForApplication(string application)
        {
            if (string.IsNullOrWhiteSpace(application))
                return BadRequest(new { error = "Application is required." });

            var query = _faults.GetAll()
                .Where(f => string.Equals(f.ApplicationName, application, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.LastTimestamp)
                .Select(f => new
                {
                    id = f.FaultId,
                    code = f.FaultCode,
                    description = f.Description,
                    severity = f.Severity,
                    type = f.Type,
                    typeDescription = f.TypeDescription,
                    timestamp = f.Timestamp
                });

            return Ok(new { faults = query });
        }

        [HttpGet]
        public IActionResult GetFaults([FromQuery] string application = null)
        {
            var query = _faults.GetAll().AsEnumerable();

            // If application filter provided → filter faults
            if (!string.IsNullOrWhiteSpace(application))
            {
                query = query.Where(f =>
                    string.Equals(f.ApplicationName, application, StringComparison.OrdinalIgnoreCase));
            }

            var items = query
                .OrderByDescending(f => f.LastTimestamp)
                .Select(f => new
                {
                    id = f.FaultId,
                    code = f.FaultCode,
                    description = f.Description,
                    severity = f.Severity,
                    type = f.Type,
                    typeDescription = f.TypeDescription,
                    timestamp = f.Timestamp,
                    // Add these back if needed:
                    //package = f.PackageFile,
                    //timeseries = f.TimeSeries
                });

            return Ok(new { faults = items });
        }

        [HttpGet("{id}")]
        public IActionResult GetFaultDetails(Guid id)
        {
            var fault = _faults.GetAll()
                .FirstOrDefault(f => f.FaultId == id);

            if (fault == null)
                return NotFound(new { error = $"Fault '{id}' not found." });

            var result = new
            {
                id = fault.FaultId,
                code = fault.FaultCode,
                description = fault.Description,
                severity = fault.Severity,
                type = fault.Type,
                typeDescription = fault.TypeDescription,
                timestamp = fault.Timestamp,
                package = fault.PackageFile,
                timeseries = fault.TimeSeries,
                application = fault.ApplicationName
            };

            return Ok(result);
        }


        [HttpGet("package/{file}")]
        public IActionResult GetPackage(string file)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagnosticsPackages", file);

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "application/zip", file);
        }
    }
}
