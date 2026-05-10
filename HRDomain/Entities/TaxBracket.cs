namespace internalEmployee.Data.Entities;

public sealed class TaxBracket
{
    public int Id { get; set; }
    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public decimal Percentage { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
