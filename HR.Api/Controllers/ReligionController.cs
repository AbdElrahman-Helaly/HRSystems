using internalEmployee.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ReligionController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ReligionLookupResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<ReligionLookupResponse>> GetReligions()
    {
        var religions = Enum.GetValues<Religion>()
            .Select(r => new ReligionLookupResponse
            {
                Id = (int)r,
                Value = r.ToString()
            })
            .ToList();

        return Ok(religions);
    }
}

public sealed class ReligionLookupResponse
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
