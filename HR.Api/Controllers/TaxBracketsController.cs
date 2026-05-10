using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TaxBracketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TaxBracketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TaxBracket>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TaxBracket>>> GetActiveBrackets(CancellationToken ct)
    {
        var brackets = await _db.TaxBrackets
            .Where(b => b.IsActive)
            .OrderBy(b => b.Order)
            .ToListAsync(ct);

        return Ok(brackets);
    }

    [HttpGet("calculate/{salary}")]
    [ProducesResponseType(typeof(TaxCalculationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaxCalculationResponse>> CalculateTax(decimal salary, CancellationToken ct)
    {
        if (salary < 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Salary must be greater than or equal to 0.");

        // Convert monthly salary to annual salary
        var annualSalary = salary * 12m;
        
        var taxBrackets = await _db.TaxBrackets
            .Where(b => b.IsActive)
            .OrderBy(b => b.Order)
            .ToListAsync(ct);

        decimal remainingSalary = annualSalary;

        // Step 1: Deduct all exempt brackets (0% tax) from the salary
        foreach (var bracket in taxBrackets)
        {
            if (bracket.Percentage == 0m)
            {
                if (bracket.ToAmount.HasValue)
                {
                    var bracketRange = bracket.ToAmount.Value - bracket.FromAmount;
                    
                    if (remainingSalary >= bracketRange)
                    {
                        remainingSalary -= bracketRange;
                    }
                    else
                    {
                        // Salary falls within this exempt bracket, no tax
                        return Ok(new TaxCalculationResponse
                        {
                            Salary = salary,
                            TaxAmount = 0m,
                            TaxPercentage = 0m,
                            ApplicableBracket = bracket
                        });
                    }
                }
            }
        }

        // Step 2: Find which bracket the remaining salary falls into
        foreach (var bracket in taxBrackets)
        {
            // Skip exempt brackets (already deducted)
            if (bracket.Percentage == 0m)
                continue;

            if (bracket.ToAmount.HasValue)
            {
                // Check if remaining salary falls within this bracket's range
                if (remainingSalary >= bracket.FromAmount && remainingSalary <= bracket.ToAmount.Value)
                {
                    // Apply this bracket's tax rate to the entire remaining salary
                    var annualTax = remainingSalary * bracket.Percentage / 100m;
                    var monthlyTax = annualTax / 12m;

                    return Ok(new TaxCalculationResponse
                    {
                        Salary = salary,
                        TaxAmount = Math.Round(monthlyTax, 2),
                        TaxPercentage = bracket.Percentage,
                        ApplicableBracket = bracket
                    });
                }
            }
            else
            {
                // Last bracket (no upper limit)
                if (remainingSalary >= bracket.FromAmount)
                {
                    // Apply this bracket's tax rate to the entire remaining salary
                    var annualTax = remainingSalary * bracket.Percentage / 100m;
                    var monthlyTax = annualTax / 12m;

                    return Ok(new TaxCalculationResponse
                    {
                        Salary = salary,
                        TaxAmount = Math.Round(monthlyTax, 2),
                        TaxPercentage = bracket.Percentage,
                        ApplicableBracket = bracket
                    });
                }
            }
        }

        // If no bracket matches, return 0
        return Ok(new TaxCalculationResponse
        {
            Salary = salary,
            TaxAmount = 0m,
            TaxPercentage = 0m,
            ApplicableBracket = null
        });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(TaxBracket), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaxBracket>> CreateBracket([FromBody] CreateTaxBracketRequest request, CancellationToken ct)
    {
        if (request.FromAmount < 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "FromAmount must be greater than or equal to 0.");

        if (request.ToAmount.HasValue && request.ToAmount.Value <= request.FromAmount)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "ToAmount must be greater than FromAmount.");

        if (request.Percentage < 0 || request.Percentage > 100)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Percentage must be between 0 and 100.");

        // Check for overlapping brackets
        var existingBrackets = await _db.TaxBrackets
            .Where(b => b.IsActive)
            .ToListAsync(ct);

        foreach (var existing in existingBrackets)
        {
            // Check if new bracket overlaps with existing bracket
            bool overlaps = false;
            if (request.ToAmount.HasValue && existing.ToAmount.HasValue)
            {
                overlaps = (request.FromAmount >= existing.FromAmount && request.FromAmount <= existing.ToAmount.Value) ||
                          (request.ToAmount.Value >= existing.FromAmount && request.ToAmount.Value <= existing.ToAmount.Value) ||
                          (request.FromAmount <= existing.FromAmount && request.ToAmount.Value >= existing.ToAmount.Value);
            }
            else if (request.ToAmount.HasValue)
            {
                overlaps = request.FromAmount <= existing.FromAmount || request.ToAmount.Value >= existing.FromAmount;
            }
            else if (existing.ToAmount.HasValue)
            {
                overlaps = request.FromAmount <= existing.ToAmount.Value;
            }
            else
            {
                overlaps = true; // Both have no upper limit
            }

            if (overlaps)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Tax bracket overlaps with existing active bracket.");
        }

        // Determine order (if not provided, use max order + 1)
        int order = request.Order;
        if (order == 0)
        {
            var maxOrder = existingBrackets.Any() ? existingBrackets.Max(b => b.Order) : 0;
            order = maxOrder + 1;
        }

        var bracket = new TaxBracket
        {
            FromAmount = request.FromAmount,
            ToAmount = request.ToAmount,
            Percentage = request.Percentage,
            Order = order,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.TaxBrackets.Add(bracket);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetActiveBrackets), new { id = bracket.Id }, bracket);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(TaxBracket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaxBracket>> UpdateBracket(int id, [FromBody] UpdateTaxBracketRequest request, CancellationToken ct)
    {
        var bracket = await _db.TaxBrackets.FindAsync(new object[] { id }, ct);
        if (bracket == null)
            return NotFound();

        if (request.FromAmount.HasValue)
        {
            if (request.FromAmount.Value < 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "FromAmount must be greater than or equal to 0.");
            bracket.FromAmount = request.FromAmount.Value;
        }

        if (request.ToAmount.HasValue)
        {
            if (request.ToAmount.Value <= bracket.FromAmount)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "ToAmount must be greater than FromAmount.");
            bracket.ToAmount = request.ToAmount;
        }

        if (request.Percentage.HasValue)
        {
            if (request.Percentage.Value < 0 || request.Percentage.Value > 100)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Percentage must be between 0 and 100.");
            bracket.Percentage = request.Percentage.Value;
        }

        if (request.Order.HasValue)
        {
            bracket.Order = request.Order.Value;
        }

        if (request.IsActive.HasValue)
        {
            bracket.IsActive = request.IsActive.Value;
        }

        bracket.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(bracket);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBracket(int id, CancellationToken ct)
    {
        var bracket = await _db.TaxBrackets.FindAsync(new object[] { id }, ct);
        if (bracket == null)
            return NotFound();

        _db.TaxBrackets.Remove(bracket);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public sealed class CreateTaxBracketRequest
{
    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public decimal Percentage { get; set; }
    public int Order { get; set; }
}

public sealed class UpdateTaxBracketRequest
{
    public decimal? FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public decimal? Percentage { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TaxCalculationResponse
{
    public decimal Salary { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public TaxBracket? ApplicableBracket { get; set; }
}
