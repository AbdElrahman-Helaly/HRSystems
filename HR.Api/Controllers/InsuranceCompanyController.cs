using internalEmployee.Services.InsuranceCompany;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InsuranceCompanyController : ControllerBase
{
    private readonly IInsuranceCompanyService _insuranceCompanyService;

    public InsuranceCompanyController(IInsuranceCompanyService insuranceCompanyService)
    {
        _insuranceCompanyService = insuranceCompanyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<internalEmployee.Data.Entities.InsuranceCompany>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<internalEmployee.Data.Entities.InsuranceCompany>>> GetAll(CancellationToken ct)
    {
        var companies = await _insuranceCompanyService.GetAllAsync(ct);
        return Ok(companies);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(internalEmployee.Data.Entities.InsuranceCompany), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<internalEmployee.Data.Entities.InsuranceCompany>> GetById(int id, CancellationToken ct)
    {
        var company = await _insuranceCompanyService.GetByIdAsync(id, ct);
        
        if (company == null)
            return NotFound(new { message = "Insurance company not found." });

        return Ok(company);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(internalEmployee.Data.Entities.InsuranceCompany), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<internalEmployee.Data.Entities.InsuranceCompany>> Create([FromBody] internalEmployee.Auth.Contracts.CreateInsuranceCompanyRequest request, CancellationToken ct)
    {
        try
        {
            var company = await _insuranceCompanyService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(internalEmployee.Data.Entities.InsuranceCompany), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<internalEmployee.Data.Entities.InsuranceCompany>> Update(int id, [FromBody] internalEmployee.Auth.Contracts.UpdateInsuranceCompanyRequest request, CancellationToken ct)
    {
        try
        {
            var company = await _insuranceCompanyService.UpdateAsync(id, request, ct);
            return Ok(company);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _insuranceCompanyService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
