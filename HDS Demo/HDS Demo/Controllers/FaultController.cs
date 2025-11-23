using HDS_Demo.Models;
using HDS_Demo.Server;
using Microsoft.AspNetCore.Mvc;

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
    }
}
