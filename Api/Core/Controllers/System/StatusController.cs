using Microsoft.AspNetCore.Mvc;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public ActionResult<StatusResponse> GetStatus()
    {
        return Ok(new StatusResponse());
    }
}