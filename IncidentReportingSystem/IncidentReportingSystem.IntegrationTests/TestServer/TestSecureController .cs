using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.IntegrationTests.TestServer
{
    [ApiController]
    [Route("__test")]
    public sealed class TestSecureController : ControllerBase
    {
        [HttpGet("secure")]
        [Authorize(Policy = "CanReadIncidents")]
        public IActionResult Get() => Ok(new { ok = true });
    }
}
